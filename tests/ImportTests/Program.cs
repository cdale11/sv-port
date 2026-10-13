using System.Text.Json.Nodes;

static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
string root = Path.Combine(Path.GetTempPath(), "sdv-import-" + Guid.NewGuid());
Directory.CreateDirectory(root);
try
{
    AssemblyHandleTests.Run(root);
    ManagedPeTests.Run(root);
    foreach (string rid in new[] { "win-x64", "linux-x64", "linux-arm64" })
    foreach (string version in new[] { "6.0.32", "6.0.36" })
    {
        string dir = Path.Combine(root, rid + version); Directory.CreateDirectory(dir);
        string deps = Path.Combine(dir, "game.deps.json"), config = Path.Combine(dir, "game.runtimeconfig.json");
        string pack = "runtimepack.Microsoft.NETCore.App.Runtime." + rid;
        string key = pack + "/" + version;
        string target = ".NETCoreApp,Version=v6.0/" + rid;
        var runtimeFiles = new JsonObject { ["System.Private.CoreLib.dll"] = new JsonObject() };
        var libs = new JsonObject {
            [key] = new JsonObject { ["runtime"] = runtimeFiles },
            ["Stardew Valley/1.6.15"] = new JsonObject { ["dependencies"] = new JsonObject { [pack] = version, ["GameLibrary"] = "1.0" } }
        };
        File.WriteAllText(deps, new JsonObject {
            ["runtimeTarget"] = new JsonObject { ["name"] = target },
            ["targets"] = new JsonObject { [target] = libs },
            ["libraries"] = new JsonObject { [key] = new JsonObject(), ["GameLibrary/1.0"] = new JsonObject() }
        }.ToJsonString());
        File.WriteAllText(config, new JsonObject { ["runtimeOptions"] = new JsonObject {
            ["tfm"] = "net6.0", ["includedFrameworks"] = new JsonArray(new JsonObject { ["name"] = "Microsoft.NETCore.App", ["version"] = version })
        } }.ToJsonString());
        File.WriteAllText(Path.Combine(dir, "System.Private.CoreLib.dll"), "runtime");
        File.WriteAllText(Path.Combine(dir, "GameLibrary.dll"), "preserve");
        GameImport.ValidateMainline(config, deps);
        GameImport.PruneRuntimePackManagedFiles(deps, dir);
        GameImport.PatchDeps(deps); GameImport.PatchRuntimeConfig(config);
        var properties = JsonNode.Parse(File.ReadAllText(config))!["runtimeOptions"]!["configProperties"]!;
        Check(properties["System.Runtime.TieredCompilation"]!.GetValue<bool>(), "tiered compilation not applied");
        Check(properties["System.Runtime.TieredCompilation.QuickJitForLoops"]!.GetValue<bool>(), "quick loop JIT not applied");
        Check(!File.Exists(Path.Combine(dir, "System.Private.CoreLib.dll")), "stale runtime");
        Check(File.Exists(Path.Combine(dir, "GameLibrary.dll")), "game library deleted");
        Check(!File.ReadAllText(deps).Contains("runtimepack."), "runtimepack still referenced");
        string once = File.ReadAllText(deps);
        GameImport.ValidateMainline(config, deps); GameImport.PatchDeps(deps);
        Check(File.ReadAllText(deps) == once, "import is not idempotent");
        File.WriteAllText(config, File.ReadAllText(config).Replace("net6.0", "net8.0"));
        bool rejected = false;
        try { GameImport.ValidateMainline(config, deps); } catch (InvalidDataException) { rejected = true; }
        Check(rejected, "future incompatible game was accepted");
    }
    string native = Path.Combine(root, "native"); Directory.CreateDirectory(native);
    foreach (ushort arch in new ushort[] { 62, 183 })
    {
        var elf = new byte[64]; elf[0] = 0x7f; elf[1] = (byte)'E'; elf[2] = (byte)'L'; elf[3] = (byte)'F'; elf[5] = 1;
        BitConverter.GetBytes(arch).CopyTo(elf, 18);
        File.WriteAllBytes(Path.Combine(native, arch + ".so"), elf);
    }
    File.WriteAllText(Path.Combine(native, "Content.xnb"), "content");
    GameImport.DeleteConflictingRuntimeFiles(native);
    Check(!File.Exists(Path.Combine(native, "62.so")), "x64 native not removed");
    Check(File.Exists(Path.Combine(native, "183.so")), "ARM64 native removed");
    Check(File.Exists(Path.Combine(native, "Content.xnb")), "content removed");
    Console.WriteLine("PASS: Windows/Linux runtime imports, version guard, idempotence, native architecture filtering");
}
finally { Directory.Delete(root, true); }
