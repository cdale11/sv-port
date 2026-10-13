using Mono.Cecil;
using Mono.Cecil.Cil;
using static GameImport;


if (!TryParseArguments(args, out var gameDir, out var overlayDir, out var smapiBundleDir, out var smapiPatchAssembly, out var modsDir))
{
    Console.Error.WriteLine("Usage: MainlineGameDataPatcher --game-dir <path> --overlay-dir <path> [--smapi-bundle-dir <path>] [--smapi-patch-assembly <path>] [--mods-dir <path>]");
    return 1;
}

gameDir = Path.GetFullPath(gameDir);
overlayDir = Path.GetFullPath(overlayDir);
smapiBundleDir = string.IsNullOrWhiteSpace(smapiBundleDir) ? string.Empty : Path.GetFullPath(smapiBundleDir);
smapiPatchAssembly = string.IsNullOrWhiteSpace(smapiPatchAssembly) ? string.Empty : Path.GetFullPath(smapiPatchAssembly);
modsDir = string.IsNullOrWhiteSpace(modsDir) ? string.Empty : Path.GetFullPath(modsDir);

if (!Directory.Exists(gameDir))
{
    Console.Error.WriteLine($"Game directory not found: {gameDir}");
    return 1;
}

EnsureRequiredFile(gameDir, "Stardew Valley.dll");
EnsureRequiredFile(gameDir, "Stardew Valley.deps.json");
EnsureRequiredFile(gameDir, "Stardew Valley.runtimeconfig.json");
if (!Directory.Exists(Path.Combine(gameDir, "Content")))
    throw new DirectoryNotFoundException("Incomplete game import: missing Content directory.");

var depsPath = Path.Combine(gameDir, "Stardew Valley.deps.json");
var runtimeConfigPath = Path.Combine(gameDir, "Stardew Valley.runtimeconfig.json");

ValidateMainline(runtimeConfigPath, depsPath);
// Resolve every expected game method before deleting bundled runtime files.
PatchGameAssemblyMethods(gameDir);
PruneRuntimePackManagedFiles(depsPath, gameDir);
DeleteConflictingRuntimeFiles(gameDir);
PatchRuntimeConfig(runtimeConfigPath);
PatchDeps(depsPath);
CopyOverlayFiles(overlayDir, gameDir);
PrepareOptionalSmapiRuntime(gameDir, depsPath, smapiBundleDir, smapiPatchAssembly);
AlignGameAssemblyVersionsForSmapi(gameDir);
NormalizeManagedAssemblies(gameDir);
NormalizeManagedAssemblies(modsDir, SearchOption.AllDirectories);

Console.WriteLine($"Prepared Stardew Valley mainline runtime in {gameDir}");
return 0;

static bool TryParseArguments(string[] args, out string gameDir, out string overlayDir, out string smapiBundleDir, out string smapiPatchAssembly, out string modsDir)
{
    gameDir = string.Empty;
    overlayDir = string.Empty;
    smapiBundleDir = string.Empty;
    smapiPatchAssembly = string.Empty;
    modsDir = string.Empty;

    for (var i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--game-dir" when i + 1 < args.Length:
                gameDir = args[++i];
                break;
            case "--overlay-dir" when i + 1 < args.Length:
                overlayDir = args[++i];
                break;
            case "--smapi-bundle-dir" when i + 1 < args.Length:
                smapiBundleDir = args[++i];
                break;
            case "--smapi-patch-assembly" when i + 1 < args.Length:
                smapiPatchAssembly = args[++i];
                break;
            case "--mods-dir" when i + 1 < args.Length:
                modsDir = args[++i];
                break;
            default:
                return false;
        }
    }

    return !string.IsNullOrWhiteSpace(gameDir) && !string.IsNullOrWhiteSpace(overlayDir);
}

static void EnsureRequiredFile(string gameDir, string relativePath)
{
    var path = Path.Combine(gameDir, relativePath);
    if (!File.Exists(path))
    {
        throw new FileNotFoundException($"Required game file is missing: {relativePath}", path);
    }
}

static void PrepareOptionalSmapiRuntime(string gameDir, string baseDepsPath, string smapiBundleDir, string smapiPatchAssembly)
{
    if (string.IsNullOrWhiteSpace(smapiBundleDir))
    {
        return;
    }

    if (!Directory.Exists(smapiBundleDir))
    {
        throw new DirectoryNotFoundException($"SMAPI bundle directory not found: {smapiBundleDir}");
    }

    EnsureRequiredFile(smapiBundleDir, "StardewModdingAPI.dll");
    EnsureRequiredFile(smapiBundleDir, "StardewModdingAPI.runtimeconfig.json");
    if (!Directory.Exists(Path.Combine(smapiBundleDir, "smapi-internal")))
    {
        throw new DirectoryNotFoundException($"SMAPI bundle is missing smapi-internal: {smapiBundleDir}");
    }

    CopyDirectoryContents(smapiBundleDir, gameDir);

    var smapiDepsPath = Path.Combine(gameDir, "StardewModdingAPI.deps.json");
    var smapiRuntimeConfigPath = Path.Combine(gameDir, "StardewModdingAPI.runtimeconfig.json");
    File.Copy(baseDepsPath, smapiDepsPath, overwrite: true);
    PatchRuntimeConfig(smapiRuntimeConfigPath);

    if (!string.IsNullOrWhiteSpace(smapiPatchAssembly))
    {
        if (!File.Exists(smapiPatchAssembly))
        {
            throw new FileNotFoundException("SMAPI patch assembly source is missing.", smapiPatchAssembly);
        }

        var patchTargetPath = Path.Combine(gameDir, "smapi-internal", "StardewPatches.dll");
        Directory.CreateDirectory(Path.GetDirectoryName(patchTargetPath) ?? gameDir);
        File.Copy(smapiPatchAssembly, patchTargetPath, overwrite: true);
    }
}

static void AlignGameAssemblyVersionsForSmapi(string gameDir)
{
    var smapiAssemblyPath = Path.Combine(gameDir, "StardewModdingAPI.dll");
    if (!File.Exists(smapiAssemblyPath))
    {
        return;
    }

    using var scope = new AssemblyReadScope(smapiAssemblyPath, gameDir);
    var smapiAssembly = scope.Assembly;
    var expectedVersions = smapiAssembly.MainModule.AssemblyReferences
        .Where(reference => reference.Name is "Stardew Valley" or "StardewValley.GameData")
        .ToDictionary(reference => reference.Name, reference => reference.Version);

    AlignAssemblyVersion(gameDir, "Stardew Valley.dll", expectedVersions);
    AlignAssemblyVersion(gameDir, "StardewValley.GameData.dll", expectedVersions);
}

static void AlignAssemblyVersion(string gameDir, string fileName, IReadOnlyDictionary<string, Version> expectedVersions)
{
    var assemblyPath = Path.Combine(gameDir, fileName);
    if (!File.Exists(assemblyPath))
    {
        return;
    }

    var assemblyName = Path.GetFileNameWithoutExtension(fileName);
    if (!expectedVersions.TryGetValue(assemblyName, out var expectedVersion))
    {
        return;
    }

    using var scope = new AssemblyReadScope(assemblyPath, gameDir);
    var assembly = scope.Assembly;
    if (assembly.Name.Version == expectedVersion)
    {
        return;
    }

    var tempPath = assemblyPath + ".version.tmp";
    assembly.Name.Version = expectedVersion;
    assembly.Write(tempPath, new WriterParameters { WriteSymbols = false });
    File.Move(tempPath, assemblyPath, overwrite: true);
    Console.WriteLine($"Aligned {assemblyName} assembly version to {expectedVersion}");
}

static void NormalizeManagedAssemblies(string rootDir, SearchOption searchOption = SearchOption.TopDirectoryOnly)
{
    if (string.IsNullOrWhiteSpace(rootDir) || !Directory.Exists(rootDir))
    {
        return;
    }

    foreach (var filePath in Directory.EnumerateFiles(rootDir, "*", searchOption).Where(path =>
                 path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                 path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)))
    {
        if (Path.GetFileName(filePath).Equals("MonoGame.Framework.dll", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (!ManagedPe.NeedsArchitectureNormalization(filePath))
        {
            continue;
        }

        NormalizeAssemblyArchitecture(filePath);
    }
}

static void PatchGameAssemblyMethods(string gameDir)
{
    var assemblyPath = Path.Combine(gameDir, "Stardew Valley.dll");
    var tempPath = assemblyPath + ".methods.tmp";
    using var scope = new AssemblyReadScope(assemblyPath, gameDir);
    var assembly = scope.Assembly;
    var module = assembly.MainModule;

    var optionsType = GetRequiredType(module, "StardewValley.Options");
    var game1Type = GetRequiredType(module, "StardewValley.Game1");
    var programType = GetRequiredType(module, "StardewValley.Program");
    var localizedContentManagerType = GetRequiredType(module, "StardewValley.LocalizedContentManager");
    var titleMenuType = GetRequiredType(module, "StardewValley.Menus.TitleMenu");
    var audioEngineWrapperType = GetRequiredType(module, "StardewValley.Audio.AudioEngineWrapper");
    var dummyAudioCategoryType = GetRequiredType(module, "StardewValley.Audio.DummyAudioCategory");
    var inputStateType = GetRequiredType(module, "StardewValley.InputState");
    var toggleFullscreen = GetRequiredMethod(game1Type, "toggleFullscreen", 0);
    var toggleNonBorderlessWindowedFullscreen = GetRequiredMethod(game1Type, "toggleNonBorderlessWindowedFullscreen", 0);
    var inputStateUpdateStates = GetRequiredMethod(inputStateType, "UpdateStates", 0);
    var inputStateSetMousePosition = GetRequiredMethod(inputStateType, "SetMousePosition", 2);
    var optionsUiScaleGetter = GetRequiredMethod(optionsType, "get_uiScale", 0);
    var optionsZoomLevelGetter = GetRequiredMethod(optionsType, "get_zoomLevel", 0);

    var game1SingletonField = GetRequiredField(game1Type, "game1");
    var game1ContentField = GetRequiredField(game1Type, "content");
    var game1UiViewportField = GetRequiredField(game1Type, "uiViewport");
    var game1ViewportField = GetRequiredField(game1Type, "viewport");
    var game1ZoomModifierField = GetRequiredField(game1Type, "zoomModifier");
    var game1TakingMapScreenshotField = GetRequiredField(game1Type, "takingMapScreenshot");
    var optionsBaseUiScaleField = GetRequiredField(optionsType, "baseUIScale");
    var optionsBaseZoomLevelField = GetRequiredField(optionsType, "baseZoomLevel");
    var inputStateSimulatedMousePositionField = GetRequiredField(inputStateType, "_simulatedMousePosition");
    var inputStateCurrentKeyboardStateField = GetRequiredField(inputStateType, "_currentKeyboardState");
    var inputStateCurrentMouseStateField = GetRequiredField(inputStateType, "_currentMouseState");
    var inputStateCurrentGamepadStateField = GetRequiredField(inputStateType, "_currentGamepadState");
    var optionsFullscreenField = GetRequiredField(optionsType, "fullscreen");
    var optionsWindowedBorderlessFullscreenField = GetRequiredField(optionsType, "windowedBorderlessFullscreen");

    var game1GetOptions = GetRequiredMethod(game1Type, "get_options", 0);
    var getWindow = GetRequiredReferencedMethodFromAny(new[] { toggleFullscreen, optionsUiScaleGetter, optionsZoomLevelGetter }, "StardewValley.InstanceGame", "get_Window");
    var getClientBounds = GetRequiredReferencedMethodFromAny(new[] { toggleFullscreen, optionsUiScaleGetter, optionsZoomLevelGetter }, "Microsoft.Xna.Framework.GameWindow", "get_ClientBounds");
    var rectangleWidthField = GetRequiredReferencedFieldFromAny(new[] { toggleFullscreen, optionsUiScaleGetter, optionsZoomLevelGetter }, "Microsoft.Xna.Framework.Rectangle", "Width");
    var rectangleHeightField = GetRequiredReferencedFieldFromAny(new[] { toggleFullscreen, optionsUiScaleGetter, optionsZoomLevelGetter }, "Microsoft.Xna.Framework.Rectangle", "Height");
    var pointXField = GetRequiredReferencedField(inputStateSetMousePosition, "Microsoft.Xna.Framework.Point", "X");
    var pointYField = GetRequiredReferencedField(inputStateSetMousePosition, "Microsoft.Xna.Framework.Point", "Y");
    var keyboardGetState = GetRequiredReferencedMethod(inputStateUpdateStates, "Microsoft.Xna.Framework.Input.Keyboard", "GetState");
    var gamePadGetState = GetRequiredReferencedMethod(inputStateUpdateStates, "Microsoft.Xna.Framework.Input.GamePad", "GetState");
    var game1GetPlayerOneIndex = GetRequiredReferencedMethod(inputStateUpdateStates, "StardewValley.Game1", "get_playerOneIndex");
    var instanceGameIsMainInstance = GetRequiredReferencedMethod(inputStateSetMousePosition, "StardewValley.InstanceGame", "get_IsMainInstance");
    var mouseStateScrollWheelValue = GetRequiredReferencedMethod(inputStateSetMousePosition, "Microsoft.Xna.Framework.Input.MouseState", "get_ScrollWheelValue");
    var mouseStateLeftButton = GetRequiredReferencedMethod(inputStateSetMousePosition, "Microsoft.Xna.Framework.Input.MouseState", "get_LeftButton");
    var mouseStateMiddleButton = GetRequiredReferencedMethod(inputStateSetMousePosition, "Microsoft.Xna.Framework.Input.MouseState", "get_MiddleButton");
    var mouseStateRightButton = GetRequiredReferencedMethod(inputStateSetMousePosition, "Microsoft.Xna.Framework.Input.MouseState", "get_RightButton");
    var mouseStateXButton1 = GetRequiredReferencedMethod(inputStateSetMousePosition, "Microsoft.Xna.Framework.Input.MouseState", "get_XButton1");
    var mouseStateXButton2 = GetRequiredReferencedMethod(inputStateSetMousePosition, "Microsoft.Xna.Framework.Input.MouseState", "get_XButton2");
    var mouseStateCtor = GetRequiredReferencedConstructor(inputStateSetMousePosition, "Microsoft.Xna.Framework.Input.MouseState", 8);
    var getRootDirectory = GetRequiredReferencedMethod(
        GetRequiredMethod(localizedContentManagerType, "GetContentRoot", 0),
        "Microsoft.Xna.Framework.Content.ContentManager",
        "get_RootDirectory");
    var mathFMin = module.ImportReference(
        typeof(MathF).GetMethod(nameof(MathF.Min), new[] { typeof(float), typeof(float) })
        ?? throw new InvalidOperationException("Unable to import MathF.Min"));
    var portDisplayWidthMethod = EnsurePortMasterDisplayDimensionMethod(programType, module, "PortMasterGetDisplayWidth", "DISPLAY_WIDTH");
    var portDisplayHeightMethod = EnsurePortMasterDisplayDimensionMethod(programType, module, "PortMasterGetDisplayHeight", "DISPLAY_HEIGHT");

    RewriteUiScaleGetter(
        optionsUiScaleGetter,
        game1SingletonField,
        game1ZoomModifierField,
        optionsBaseUiScaleField,
        getWindow,
        getClientBounds,
        rectangleWidthField,
        rectangleHeightField,
        mathFMin,
        portDisplayWidthMethod,
        portDisplayHeightMethod);

    RewriteZoomLevelGetter(
        optionsZoomLevelGetter,
        game1SingletonField,
        game1ZoomModifierField,
        game1TakingMapScreenshotField,
        optionsBaseZoomLevelField,
        getWindow,
        getClientBounds,
        rectangleWidthField,
        rectangleHeightField,
        mathFMin,
        portDisplayWidthMethod,
        portDisplayHeightMethod);

    RewriteContentRootGetter(
        GetRequiredMethod(localizedContentManagerType, "GetContentRoot", 0),
        game1ContentField,
        getRootDirectory);

    RewriteAudioEngineWrapperGetCategory(
        GetRequiredMethod(audioEngineWrapperType, "GetCategory", 1),
        module.ImportReference(
            dummyAudioCategoryType.Methods.FirstOrDefault(m => m.Name == ".ctor" && !m.IsStatic && m.Parameters.Count == 0)
            ?? throw new InvalidOperationException("Unable to find StardewValley.Audio.DummyAudioCategory::.ctor()")));

    RewriteTitleMenuViewportUsage(
        GetRequiredMethod(titleMenuType, "gameWindowSizeChanged", 2),
        game1ViewportField,
        game1UiViewportField,
        1);

    RewriteTitleMenuViewportUsage(
        GetRequiredMethod(titleMenuType, "update", 1),
        game1ViewportField,
        game1UiViewportField,
        2);
    PatchOutTitleMenuWindowedButtonUpdatePosition(GetRequiredMethod(titleMenuType, "update", 1));

    RewriteInputStateUpdateStates(
        inputStateUpdateStates,
        inputStateCurrentKeyboardStateField,
        inputStateCurrentGamepadStateField,
        keyboardGetState,
        game1GetPlayerOneIndex,
        gamePadGetState);

    RewriteInputStateSetMousePosition(
        inputStateSetMousePosition,
        game1SingletonField,
        inputStateSimulatedMousePositionField,
        inputStateCurrentMouseStateField,
        pointXField,
        pointYField,
        instanceGameIsMainInstance,
        mouseStateScrollWheelValue,
        mouseStateLeftButton,
        mouseStateMiddleButton,
        mouseStateRightButton,
        mouseStateXButton1,
        mouseStateXButton2,
        mouseStateCtor);

    RewriteBooleanReturn(GetRequiredMethod(optionsType, "isCurrentlyWindowedBorderless", 0), true);
    RewriteBooleanReturn(GetRequiredMethod(optionsType, "isCurrentlyFullscreen", 0), false);
    RewriteBooleanReturn(GetRequiredMethod(optionsType, "isCurrentlyWindowed", 0), false);
    RewriteWindowModeOptionSetter(GetRequiredMethodByParameterType(optionsType, "setWindowedOption", "System.String"), optionsFullscreenField, optionsWindowedBorderlessFullscreenField);
    RewriteWindowModeOptionSetter(GetRequiredMethodByParameterType(optionsType, "setWindowedOption", "System.Int32"), optionsFullscreenField, optionsWindowedBorderlessFullscreenField);
    RewriteGameWindowModeToggle(toggleFullscreen, game1GetOptions, optionsFullscreenField, optionsWindowedBorderlessFullscreenField);
    RewriteGameWindowModeToggle(toggleNonBorderlessWindowedFullscreen, game1GetOptions, optionsFullscreenField, optionsWindowedBorderlessFullscreenField);
    var setWindowSize = GetRequiredMethod(game1Type, "SetWindowSize", 2);
    PatchGame1SetWindowSizeDisplayDimensions(
        setWindowSize,
        game1GraphicsField: GetRequiredField(game1Type, "graphics"),
        preferredBackBufferWidthSetter: GetRequiredReferencedMethod(setWindowSize, "Microsoft.Xna.Framework.GraphicsDeviceManager", "set_PreferredBackBufferWidth"),
        preferredBackBufferHeightSetter: GetRequiredReferencedMethod(setWindowSize, "Microsoft.Xna.Framework.GraphicsDeviceManager", "set_PreferredBackBufferHeight"),
        portDisplayWidthMethod,
        portDisplayHeightMethod);
    PatchOutDisplayModeChanges(game1Type, optionsType);

    assembly.Write(tempPath, new WriterParameters { WriteSymbols = false });
    File.Move(tempPath, assemblyPath, overwrite: true);
}

static MethodDefinition EnsurePortMasterDisplayDimensionMethod(
    TypeDefinition ownerType,
    ModuleDefinition module,
    string methodName,
    string envName)
{
    var existingMethod = ownerType.Methods.FirstOrDefault(method => method.Name == methodName && method.Parameters.Count == 1);
    if (existingMethod is not null)
    {
        existingMethod.Attributes &= ~MethodAttributes.Private;
        existingMethod.Attributes |= MethodAttributes.Assembly;
        return existingMethod;
    }

    var method = new MethodDefinition(
        methodName,
        MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig,
        module.TypeSystem.Int32);
    method.Parameters.Add(new ParameterDefinition("fallback", ParameterAttributes.None, module.TypeSystem.Int32));
    method.Body.InitLocals = true;
    var parsedValue = new VariableDefinition(module.TypeSystem.Int32);
    method.Body.Variables.Add(parsedValue);
    ownerType.Methods.Add(method);

    var getEnvironmentVariable = module.ImportReference(
        typeof(Environment).GetMethod(nameof(Environment.GetEnvironmentVariable), new[] { typeof(string) })
        ?? throw new InvalidOperationException("Unable to import Environment.GetEnvironmentVariable"));
    var intTryParse = module.ImportReference(
        typeof(int).GetMethod(nameof(int.TryParse), new[] { typeof(string), typeof(int).MakeByRefType() })
        ?? throw new InvalidOperationException("Unable to import int.TryParse"));

    var il = method.Body.GetILProcessor();
    var returnFallback = il.Create(OpCodes.Ldarg_0);

    il.Append(il.Create(OpCodes.Ldstr, envName));
    il.Append(il.Create(OpCodes.Call, getEnvironmentVariable));
    il.Append(il.Create(OpCodes.Ldloca_S, parsedValue));
    il.Append(il.Create(OpCodes.Call, intTryParse));
    il.Append(il.Create(OpCodes.Brfalse_S, returnFallback));
    il.Append(il.Create(OpCodes.Ldloc_0));
    il.Append(il.Create(OpCodes.Ldc_I4_0));
    il.Append(il.Create(OpCodes.Ble_S, returnFallback));
    il.Append(il.Create(OpCodes.Ldloc_0));
    il.Append(il.Create(OpCodes.Ret));

    il.Append(returnFallback);
    il.Append(il.Create(OpCodes.Ret));

    return method;
}

static void RewriteUiScaleGetter(
    MethodDefinition method,
    FieldReference game1SingletonField,
    FieldReference zoomModifierField,
    FieldReference baseUiScaleField,
    MethodReference getWindow,
    MethodReference getClientBounds,
    FieldReference rectangleWidthField,
    FieldReference rectangleHeightField,
    MethodReference mathFMin,
    MethodReference portDisplayWidthMethod,
    MethodReference portDisplayHeightMethod)
{
    ResetMethodBody(method);
    var il = method.Body.GetILProcessor();

    il.Append(il.Create(OpCodes.Ldarg_0));
    il.Append(il.Create(OpCodes.Ldfld, baseUiScaleField));
    il.Append(il.Create(OpCodes.Ldsfld, game1SingletonField));
    il.Append(il.Create(OpCodes.Ldfld, zoomModifierField));
    il.Append(il.Create(OpCodes.Mul));
    EmitScreenRatio(il, game1SingletonField, getWindow, getClientBounds, rectangleWidthField, rectangleHeightField, mathFMin, portDisplayWidthMethod, portDisplayHeightMethod);
    il.Append(il.Create(OpCodes.Mul));
    il.Append(il.Create(OpCodes.Ret));
}

static void RewriteZoomLevelGetter(
    MethodDefinition method,
    FieldReference game1SingletonField,
    FieldReference zoomModifierField,
    FieldReference takingMapScreenshotField,
    FieldReference baseZoomLevelField,
    MethodReference getWindow,
    MethodReference getClientBounds,
    FieldReference rectangleWidthField,
    FieldReference rectangleHeightField,
    MethodReference mathFMin,
    MethodReference portDisplayWidthMethod,
    MethodReference portDisplayHeightMethod)
{
    ResetMethodBody(method);
    var il = method.Body.GetILProcessor();
    var nonScreenshot = il.Create(OpCodes.Nop);

    il.Append(il.Create(OpCodes.Ldsfld, game1SingletonField));
    il.Append(il.Create(OpCodes.Ldfld, takingMapScreenshotField));
    il.Append(il.Create(OpCodes.Brfalse_S, nonScreenshot));
    il.Append(il.Create(OpCodes.Ldarg_0));
    il.Append(il.Create(OpCodes.Ldfld, baseZoomLevelField));
    EmitScreenRatio(il, game1SingletonField, getWindow, getClientBounds, rectangleWidthField, rectangleHeightField, mathFMin, portDisplayWidthMethod, portDisplayHeightMethod);
    il.Append(il.Create(OpCodes.Mul));
    il.Append(il.Create(OpCodes.Ret));

    il.Append(nonScreenshot);
    il.Append(il.Create(OpCodes.Ldarg_0));
    il.Append(il.Create(OpCodes.Ldfld, baseZoomLevelField));
    EmitScreenRatio(il, game1SingletonField, getWindow, getClientBounds, rectangleWidthField, rectangleHeightField, mathFMin, portDisplayWidthMethod, portDisplayHeightMethod);
    il.Append(il.Create(OpCodes.Mul));
    il.Append(il.Create(OpCodes.Ldsfld, game1SingletonField));
    il.Append(il.Create(OpCodes.Ldfld, zoomModifierField));
    il.Append(il.Create(OpCodes.Mul));
    il.Append(il.Create(OpCodes.Ret));
}

static void RewriteContentRootGetter(
    MethodDefinition method,
    FieldReference game1ContentField,
    MethodReference getRootDirectory)
{
    ResetMethodBody(method);
    var il = method.Body.GetILProcessor();

    il.Append(il.Create(OpCodes.Ldsfld, game1ContentField));
    il.Append(il.Create(OpCodes.Callvirt, getRootDirectory));
    il.Append(il.Create(OpCodes.Ret));
}

static void RewriteAudioEngineWrapperGetCategory(
    MethodDefinition method,
    MethodReference dummyAudioCategoryCtor)
{
    ResetMethodBody(method);
    var il = method.Body.GetILProcessor();

    il.Append(il.Create(OpCodes.Newobj, dummyAudioCategoryCtor));
    il.Append(il.Create(OpCodes.Ret));
}

static void PatchOutDisplayModeChanges(params TypeDefinition[] types)
{
    foreach (var method in types.SelectMany(type => type.Methods).Where(method => method.HasBody))
    {
        if (method.DeclaringType.FullName == "StardewValley.Game1" && method.Name == "SetWindowSize")
        {
            continue;
        }

        foreach (var instruction in method.Body.Instructions)
        {
            if (instruction.OpCode != OpCodes.Callvirt ||
                instruction.Operand is not MethodReference methodReference ||
                methodReference.DeclaringType.FullName != "Microsoft.Xna.Framework.GraphicsDeviceManager" ||
                methodReference.Parameters.Count != 0 ||
                methodReference.ReturnType.FullName != "System.Void")
            {
                continue;
            }

            if (methodReference.Name is not ("ApplyChanges" or "ToggleFullScreen"))
            {
                continue;
            }

            instruction.OpCode = OpCodes.Pop;
            instruction.Operand = null;
        }
    }
}

static void RewriteBooleanReturn(MethodDefinition method, bool value)
{
    ResetMethodBody(method);
    var il = method.Body.GetILProcessor();
    il.Append(il.Create(value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
    il.Append(il.Create(OpCodes.Ret));
}

static void RewriteWindowModeOptionSetter(
    MethodDefinition method,
    FieldReference fullscreenField,
    FieldReference windowedBorderlessFullscreenField)
{
    ResetMethodBody(method);
    var il = method.Body.GetILProcessor();
    EmitSetWindowModeFields(il, fullscreenField, windowedBorderlessFullscreenField);
    il.Append(il.Create(OpCodes.Ret));
}

static void RewriteGameWindowModeToggle(
    MethodDefinition method,
    MethodReference getOptions,
    FieldReference fullscreenField,
    FieldReference windowedBorderlessFullscreenField)
{
    ResetMethodBody(method);
    var il = method.Body.GetILProcessor();

    il.Append(il.Create(OpCodes.Call, getOptions));
    il.Append(il.Create(OpCodes.Ldc_I4_0));
    il.Append(il.Create(OpCodes.Stfld, fullscreenField));
    il.Append(il.Create(OpCodes.Call, getOptions));
    il.Append(il.Create(OpCodes.Ldc_I4_1));
    il.Append(il.Create(OpCodes.Stfld, windowedBorderlessFullscreenField));
    il.Append(il.Create(OpCodes.Ret));
}

static void PatchGame1SetWindowSizeDisplayDimensions(
    MethodDefinition method,
    FieldReference game1GraphicsField,
    MethodReference preferredBackBufferWidthSetter,
    MethodReference preferredBackBufferHeightSetter,
    MethodReference portDisplayWidthMethod,
    MethodReference portDisplayHeightMethod)
{
    if (method.Body.Instructions.Any(instruction =>
            instruction.Operand is MethodReference reference &&
            (reference.FullName == portDisplayWidthMethod.FullName || reference.FullName == portDisplayHeightMethod.FullName)))
    {
        return;
    }

    if (method.Parameters.Count != 2)
    {
        throw new InvalidOperationException($"Unexpected SetWindowSize parameter count: {method.FullName}");
    }

    var il = method.Body.GetILProcessor();
    var first = method.Body.Instructions.FirstOrDefault()
                ?? throw new InvalidOperationException($"Unable to patch empty method: {method.FullName}");
    var injected = new[]
    {
        il.Create(OpCodes.Ldarg_1),
        il.Create(OpCodes.Call, portDisplayWidthMethod),
        il.Create(OpCodes.Starg_S, method.Parameters[0]),
        il.Create(OpCodes.Ldarg_2),
        il.Create(OpCodes.Call, portDisplayHeightMethod),
        il.Create(OpCodes.Starg_S, method.Parameters[1]),
        il.Create(OpCodes.Ldsfld, game1GraphicsField),
        il.Create(OpCodes.Ldarg_1),
        il.Create(OpCodes.Callvirt, preferredBackBufferWidthSetter),
        il.Create(OpCodes.Ldsfld, game1GraphicsField),
        il.Create(OpCodes.Ldarg_2),
        il.Create(OpCodes.Callvirt, preferredBackBufferHeightSetter),
    };

    foreach (var instruction in injected)
    {
        il.InsertBefore(first, instruction);
    }
}

static void EmitSetWindowModeFields(
    ILProcessor il,
    FieldReference fullscreenField,
    FieldReference windowedBorderlessFullscreenField)
{
    il.Append(il.Create(OpCodes.Ldarg_0));
    il.Append(il.Create(OpCodes.Ldc_I4_0));
    il.Append(il.Create(OpCodes.Stfld, fullscreenField));
    il.Append(il.Create(OpCodes.Ldarg_0));
    il.Append(il.Create(OpCodes.Ldc_I4_1));
    il.Append(il.Create(OpCodes.Stfld, windowedBorderlessFullscreenField));
}

static void RewriteTitleMenuViewportUsage(
    MethodDefinition method,
    FieldReference viewportField,
    FieldReference uiViewportField,
    int expectedReplacements)
{
    var originalViewportRefs = 0;
    var existingUiViewportRefs = 0;

    foreach (var instruction in method.Body.Instructions)
    {
        if (instruction.Operand is not FieldReference field)
        {
            continue;
        }

        if (field.FullName == viewportField.FullName)
        {
            originalViewportRefs++;
        }
        else if (field.FullName == uiViewportField.FullName)
        {
            existingUiViewportRefs++;
        }
    }

    var replacements = 0;
    foreach (var instruction in method.Body.Instructions)
    {
        if (instruction.Operand is not FieldReference field)
        {
            continue;
        }

        if (field.FullName != viewportField.FullName)
        {
            continue;
        }

        instruction.Operand = uiViewportField;
        replacements++;
    }

    if (replacements == expectedReplacements)
    {
        return;
    }

    if (replacements == 0 && originalViewportRefs == 0 && existingUiViewportRefs >= expectedReplacements)
    {
        return;
    }

    if (replacements != expectedReplacements)
    {
        throw new InvalidOperationException(
            $"Unexpected TitleMenu viewport patch count for {method.FullName}: expected {expectedReplacements}, got {replacements} (original viewport refs={originalViewportRefs}, existing uiViewport refs={existingUiViewportRefs})");
    }
}

static void PatchOutTitleMenuWindowedButtonUpdatePosition(MethodDefinition method)
{
    var setPositionCalls = method.Body.Instructions
        .Select((instruction, index) => (instruction, index))
        .Where(item => IsSetPositionCall(item.instruction) && ReferencesWindowedButtonBefore(method.Body.Instructions, item.index))
        .ToList();

    if (setPositionCalls.Count == 0)
    {
        return;
    }
    if (setPositionCalls.Count != 1)
    {
        throw new InvalidOperationException($"Unexpected TitleMenu windowedButton setPosition count in {method.FullName}: {setPositionCalls.Count}");
    }

    var il = method.Body.GetILProcessor();
    var call = setPositionCalls[0].instruction;
    call.OpCode = OpCodes.Pop;
    call.Operand = null;
    il.InsertAfter(call, il.Create(OpCodes.Pop));
}

static bool IsSetPositionCall(Instruction instruction)
{
    return instruction.OpCode == OpCodes.Callvirt &&
           instruction.Operand is MethodReference reference &&
           reference.Name == "setPosition" &&
           reference.DeclaringType.FullName is "StardewValley.Menus.ClickableComponent" or "StardewValley.Menus.ClickableTextureComponent" &&
           reference.Parameters.Count == 1 &&
           reference.Parameters[0].ParameterType.FullName == "Microsoft.Xna.Framework.Vector2";
}

static bool ReferencesWindowedButtonBefore(Mono.Collections.Generic.Collection<Instruction> instructions, int callIndex)
{
    for (var index = callIndex - 1; index >= 0 && index >= callIndex - 80; index--)
    {
        if (instructions[index].Operand is FieldReference field &&
            field.Name == "windowedButton" &&
            field.DeclaringType.FullName == "StardewValley.Menus.TitleMenu")
        {
            return true;
        }
    }

    return false;
}

static void RewriteInputStateUpdateStates(
    MethodDefinition method,
    FieldReference currentKeyboardStateField,
    FieldReference currentGamepadStateField,
    MethodReference keyboardGetState,
    MethodReference game1GetPlayerOneIndex,
    MethodReference gamePadGetState)
{
    ResetMethodBody(method);
    var il = method.Body.GetILProcessor();
    var noGamepad = il.Create(OpCodes.Nop);

    il.Append(il.Create(OpCodes.Ldarg_0));
    il.Append(il.Create(OpCodes.Call, keyboardGetState));
    il.Append(il.Create(OpCodes.Stfld, currentKeyboardStateField));

    il.Append(il.Create(OpCodes.Call, game1GetPlayerOneIndex));
    il.Append(il.Create(OpCodes.Ldc_I4_0));
    il.Append(il.Create(OpCodes.Blt_S, noGamepad));

    il.Append(il.Create(OpCodes.Ldarg_0));
    il.Append(il.Create(OpCodes.Call, game1GetPlayerOneIndex));
    il.Append(il.Create(OpCodes.Call, gamePadGetState));
    il.Append(il.Create(OpCodes.Stfld, currentGamepadStateField));
    il.Append(il.Create(OpCodes.Ret));

    il.Append(noGamepad);
    il.Append(il.Create(OpCodes.Ldarg_0));
    il.Append(il.Create(OpCodes.Ldflda, currentGamepadStateField));
    il.Append(il.Create(OpCodes.Initobj, currentGamepadStateField.FieldType));
    il.Append(il.Create(OpCodes.Ret));
}

static void RewriteInputStateSetMousePosition(
    MethodDefinition method,
    FieldReference game1SingletonField,
    FieldReference simulatedMousePositionField,
    FieldReference currentMouseStateField,
    FieldReference pointXField,
    FieldReference pointYField,
    MethodReference instanceGameIsMainInstance,
    MethodReference mouseStateScrollWheelValue,
    MethodReference mouseStateLeftButton,
    MethodReference mouseStateMiddleButton,
    MethodReference mouseStateRightButton,
    MethodReference mouseStateXButton1,
    MethodReference mouseStateXButton2,
    MethodReference mouseStateCtor)
{
    ResetMethodBody(method);
    var il = method.Body.GetILProcessor();
    var mainInstance = il.Create(OpCodes.Nop);

    il.Append(il.Create(OpCodes.Ldsfld, game1SingletonField));
    il.Append(il.Create(OpCodes.Callvirt, instanceGameIsMainInstance));
    il.Append(il.Create(OpCodes.Brtrue_S, mainInstance));

    il.Append(il.Create(OpCodes.Ldarg_0));
    il.Append(il.Create(OpCodes.Ldflda, simulatedMousePositionField));
    il.Append(il.Create(OpCodes.Ldarg_1));
    il.Append(il.Create(OpCodes.Stfld, pointXField));
    il.Append(il.Create(OpCodes.Ldarg_0));
    il.Append(il.Create(OpCodes.Ldflda, simulatedMousePositionField));
    il.Append(il.Create(OpCodes.Ldarg_2));
    il.Append(il.Create(OpCodes.Stfld, pointYField));
    il.Append(il.Create(OpCodes.Ret));

    il.Append(mainInstance);
    il.Append(il.Create(OpCodes.Ldarg_0));
    il.Append(il.Create(OpCodes.Ldarg_1));
    il.Append(il.Create(OpCodes.Ldarg_2));
    EmitMouseStateValue(il, currentMouseStateField, mouseStateScrollWheelValue);
    EmitMouseStateValue(il, currentMouseStateField, mouseStateLeftButton);
    EmitMouseStateValue(il, currentMouseStateField, mouseStateMiddleButton);
    EmitMouseStateValue(il, currentMouseStateField, mouseStateRightButton);
    EmitMouseStateValue(il, currentMouseStateField, mouseStateXButton1);
    EmitMouseStateValue(il, currentMouseStateField, mouseStateXButton2);
    il.Append(il.Create(OpCodes.Newobj, mouseStateCtor));
    il.Append(il.Create(OpCodes.Stfld, currentMouseStateField));
    il.Append(il.Create(OpCodes.Ret));
}

static void EmitMouseStateValue(ILProcessor il, FieldReference currentMouseStateField, MethodReference getter)
{
    il.Append(il.Create(OpCodes.Ldarg_0));
    il.Append(il.Create(OpCodes.Ldflda, currentMouseStateField));
    il.Append(il.Create(OpCodes.Call, getter));
}

static void EmitScreenRatio(
    ILProcessor il,
    FieldReference game1SingletonField,
    MethodReference getWindow,
    MethodReference getClientBounds,
    FieldReference rectangleWidthField,
    FieldReference rectangleHeightField,
    MethodReference mathFMin,
    MethodReference portDisplayWidthMethod,
    MethodReference portDisplayHeightMethod)
{
    il.Append(il.Create(OpCodes.Ldsfld, game1SingletonField));
    il.Append(il.Create(OpCodes.Callvirt, getWindow));
    il.Append(il.Create(OpCodes.Callvirt, getClientBounds));
    il.Append(il.Create(OpCodes.Ldfld, rectangleHeightField));
    il.Append(il.Create(OpCodes.Call, portDisplayHeightMethod));
    il.Append(il.Create(OpCodes.Conv_R4));
    il.Append(il.Create(OpCodes.Ldc_R4, 768f));
    il.Append(il.Create(OpCodes.Div));

    il.Append(il.Create(OpCodes.Ldsfld, game1SingletonField));
    il.Append(il.Create(OpCodes.Callvirt, getWindow));
    il.Append(il.Create(OpCodes.Callvirt, getClientBounds));
    il.Append(il.Create(OpCodes.Ldfld, rectangleWidthField));
    il.Append(il.Create(OpCodes.Call, portDisplayWidthMethod));
    il.Append(il.Create(OpCodes.Conv_R4));
    il.Append(il.Create(OpCodes.Ldc_R4, 1366f));
    il.Append(il.Create(OpCodes.Div));

    il.Append(il.Create(OpCodes.Call, mathFMin));
}

static void ResetMethodBody(MethodDefinition method)
{
    method.Body.ExceptionHandlers.Clear();
    method.Body.Variables.Clear();
    method.Body.InitLocals = false;
    method.Body.Instructions.Clear();
}

static TypeDefinition GetRequiredType(ModuleDefinition module, string fullName)
{
    return module.Types.FirstOrDefault(type => type.FullName == fullName)
           ?? throw new InvalidOperationException($"Unable to find type: {fullName}");
}

static MethodDefinition GetRequiredMethod(TypeDefinition type, string name, int parameterCount)
{
    return type.Methods.FirstOrDefault(method => method.Name == name && method.Parameters.Count == parameterCount)
           ?? throw new InvalidOperationException($"Unable to find method {type.FullName}::{name}({parameterCount})");
}

static MethodDefinition GetRequiredMethodByParameterType(TypeDefinition type, string name, string parameterTypeFullName)
{
    return type.Methods.FirstOrDefault(method =>
               method.Name == name &&
               method.Parameters.Count == 1 &&
               method.Parameters[0].ParameterType.FullName == parameterTypeFullName)
           ?? throw new InvalidOperationException($"Unable to find method {type.FullName}::{name}({parameterTypeFullName})");
}

static FieldDefinition GetRequiredField(TypeDefinition type, string name)
{
    return type.Fields.FirstOrDefault(field => field.Name == name)
           ?? throw new InvalidOperationException($"Unable to find field {type.FullName}::{name}");
}

static MethodReference GetRequiredReferencedMethod(MethodDefinition method, string declaringTypeFullName, string name)
{
    return method.Body.Instructions
               .Where(ins => ins.OpCode.FlowControl == FlowControl.Call && ins.Operand is MethodReference)
               .Select(ins => (MethodReference)ins.Operand)
               .FirstOrDefault(reference => reference.Name == name && reference.DeclaringType.FullName == declaringTypeFullName)
           ?? throw new InvalidOperationException($"Unable to find referenced method: {declaringTypeFullName}::{name}");
}

static MethodReference GetRequiredReferencedMethodFromAny(IEnumerable<MethodDefinition> methods, string declaringTypeFullName, string name)
{
    foreach (var method in methods)
    {
        var reference = method.Body.Instructions
            .Where(ins => ins.OpCode.FlowControl == FlowControl.Call && ins.Operand is MethodReference)
            .Select(ins => (MethodReference)ins.Operand)
            .FirstOrDefault(reference => reference.Name == name && reference.DeclaringType.FullName == declaringTypeFullName);

        if (reference is not null)
        {
            return reference;
        }
    }

    throw new InvalidOperationException($"Unable to find referenced method: {declaringTypeFullName}::{name}");
}

static MethodReference GetRequiredReferencedConstructor(MethodDefinition method, string declaringTypeFullName, int parameterCount)
{
    return method.Body.Instructions
               .Where(ins => ins.OpCode == OpCodes.Newobj && ins.Operand is MethodReference)
               .Select(ins => (MethodReference)ins.Operand)
               .FirstOrDefault(reference =>
                   reference.Name == ".ctor" &&
                   reference.DeclaringType.FullName == declaringTypeFullName &&
                   reference.Parameters.Count == parameterCount)
           ?? throw new InvalidOperationException($"Unable to find referenced constructor: {declaringTypeFullName}({parameterCount})");
}

static FieldReference GetRequiredReferencedField(MethodDefinition method, string declaringTypeFullName, string name)
{
    return method.Body.Instructions
               .Where(ins => ins.Operand is FieldReference)
               .Select(ins => (FieldReference)ins.Operand)
               .FirstOrDefault(reference => reference.Name == name && reference.DeclaringType.FullName == declaringTypeFullName)
           ?? throw new InvalidOperationException($"Unable to find referenced field: {declaringTypeFullName}::{name}");
}

static FieldReference GetRequiredReferencedFieldFromAny(IEnumerable<MethodDefinition> methods, string declaringTypeFullName, string name)
{
    foreach (var method in methods)
    {
        var reference = method.Body.Instructions
            .Where(ins => ins.Operand is FieldReference)
            .Select(ins => (FieldReference)ins.Operand)
            .FirstOrDefault(reference => reference.Name == name && reference.DeclaringType.FullName == declaringTypeFullName);

        if (reference is not null)
        {
            return reference;
        }
    }

    throw new InvalidOperationException($"Unable to find referenced field: {declaringTypeFullName}::{name}");
}

static void NormalizeAssemblyArchitecture(string assemblyPath)
{
    var tempPath = assemblyPath + ".tmp";
    using var scope = new AssemblyReadScope(assemblyPath, Path.GetDirectoryName(assemblyPath) ?? Environment.CurrentDirectory);
    var module = scope.Assembly.MainModule;
    module.Architecture = TargetArchitecture.I386;
    module.Attributes |= ModuleAttributes.ILOnly;
    module.Attributes &= ~ModuleAttributes.Required32Bit;
    module.Attributes &= ~ModuleAttributes.Preferred32Bit;
    module.Write(tempPath, new WriterParameters { WriteSymbols = false });
    File.Move(tempPath, assemblyPath, overwrite: true);
}

static void CopyDirectoryContents(string sourceDir, string destinationDir)
{
    foreach (var sourcePath in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
    {
        var relativePath = Path.GetRelativePath(sourceDir, sourcePath);
        var destinationPath = Path.Combine(destinationDir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? destinationDir);
        File.Copy(sourcePath, destinationPath, overwrite: true);
    }
}

