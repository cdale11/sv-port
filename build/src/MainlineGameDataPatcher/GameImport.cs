using System.Text.Json;
using System.Text.Json.Nodes;

// Shared by the importer and retail-free regression fixtures.
internal static class GameImport
{
    internal static void CopyOverlayFiles(string overlayDir, string gameDir)
    {
        if (!Directory.Exists(overlayDir)) return;
        foreach (var sourcePath in Directory.EnumerateFiles(overlayDir, "*", SearchOption.AllDirectories))
        {
            var destinationPath = Path.Combine(gameDir, Path.GetRelativePath(overlayDir, sourcePath));
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? gameDir);
            File.Copy(sourcePath, destinationPath, overwrite: true);
        }
    }

    private const string Target = ".NETCoreApp,Version=v6.0/linux-arm64";
    private const string PackPrefix = "runtimepack.Microsoft.NETCore.App.Runtime.";
    private static JsonObject Read(string path) => JsonNode.Parse(File.ReadAllText(path),
        documentOptions: new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip })!.AsObject();
    private static void Write(string path, JsonObject root) => File.WriteAllText(path,
        root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);

    internal static void ValidateMainline(string runtimePath, string depsPath)
    {
        var options = Read(runtimePath)["runtimeOptions"]?.AsObject()
            ?? throw new InvalidDataException("Missing runtimeOptions.");
        var target = Read(depsPath)["runtimeTarget"]?["name"]?.GetValue<string>() ?? "";
        if (options["tfm"]?.GetValue<string>() != "net6.0" ||
            !(target == ".NETCoreApp,Version=v6.0" || target.StartsWith(".NETCoreApp,Version=v6.0/", StringComparison.Ordinal)))
            throw new InvalidDataException("This port supports .NET 6 Windows/Linux mainline files. This game build needs a port update; files were not modified.");
        var frameworks = options["includedFrameworks"] as JsonArray ?? options["frameworks"] as JsonArray;
        IEnumerable<JsonNode?> selected = frameworks is null ? new[] { options["framework"] } : frameworks;
        foreach (var framework in selected.Where(f => f != null))
        {
            var version = Version.Parse(framework!["version"]!.GetValue<string>());
            if (framework["name"]?.GetValue<string>() != "Microsoft.NETCore.App" || version.Major != 6 || version.Minor != 0 || version.Build > 36)
                throw new InvalidDataException("Game requires a framework newer than the bundled .NET 6.0.36 runtime.");
        }
        var targets = Read(depsPath)["targets"]?.AsObject();
        if (targets?[target] is not JsonObject) throw new InvalidDataException("Missing active dependency target.");
    }

    internal static void PruneRuntimePackManagedFiles(string depsPath, string gameDir)
    {
        var root = Read(depsPath);
        string active = root["runtimeTarget"]!["name"]!.GetValue<string>();
        var target = root["targets"]![active]!.AsObject();
        string prefix = Path.GetFullPath(gameDir) + Path.DirectorySeparatorChar;
        foreach (var pack in target.Where(e => e.Key.StartsWith(PackPrefix, StringComparison.Ordinal)))
        {
            if (pack.Value?["runtime"] is not JsonObject files) continue;
            foreach (var file in files)
            {
                string path = Path.GetFullPath(Path.Combine(gameDir, file.Key));
                if (!path.StartsWith(prefix, StringComparison.Ordinal)) throw new InvalidDataException("Runtime path escapes gamedata.");
                File.Delete(path);
            }
        }
    }

    internal static void PatchDeps(string depsPath)
    {
        var root = Read(depsPath);
        string old = root["runtimeTarget"]!["name"]!.GetValue<string>();
        var targets = root["targets"]!.AsObject();
        var active = JsonNode.Parse(targets[old]!.ToJsonString())!.AsObject();
        foreach (var key in active.Select(e => e.Key).Where(k => k.StartsWith(PackPrefix, StringComparison.Ordinal)).ToList()) active.Remove(key);
        foreach (var lib in active)
        {
            if (lib.Value?["dependencies"] is JsonObject dependencies)
                foreach (var key in dependencies.Select(e => e.Key).Where(k => k.StartsWith(PackPrefix, StringComparison.Ordinal)).ToList()) dependencies.Remove(key);
        }
        root["targets"] = new JsonObject { [Target] = active };
        root["runtimeTarget"]!["name"] = Target;
        if (root["libraries"] is JsonObject libraries)
            foreach (var key in libraries.Select(e => e.Key).Where(k => k.StartsWith(PackPrefix, StringComparison.Ordinal)).ToList()) libraries.Remove(key);
        Write(depsPath, root);
    }

    internal static void PatchRuntimeConfig(string runtimePath)
    {
        var root = Read(runtimePath);
        var options = root["runtimeOptions"]!.AsObject();
        options.Remove("includedFrameworks");
        options.Remove("frameworks");
        options["framework"] = new JsonObject { ["name"] = "Microsoft.NETCore.App", ["version"] = "6.0.36" };
        options["rollForward"] = "LatestPatch";
        var properties = options["configProperties"] as JsonObject;
        if (properties is null) { properties = new JsonObject(); options["configProperties"] = properties; }
        properties["System.GC.Server"] = false;
        Write(runtimePath, root);
    }

    internal static void DeleteConflictingRuntimeFiles(string gameDir)
    {
        // Retail Linux ships x64 ELF libraries/apphosts. Remove only identified
        // incompatible native binaries, preserving managed DLLs and content.
        foreach (var path in Directory.EnumerateFiles(gameDir, "*", SearchOption.TopDirectoryOnly))
        {
            using var stream = File.OpenRead(path);
            byte[] header = new byte[64];
            int size = stream.Read(header, 0, header.Length);
            bool remove = size >= 20 && header[0] == 0x7f && header[1] == 'E' && header[2] == 'L' && header[3] == 'F' && header[5] == 1 &&
                (BitConverter.ToUInt16(header, 18) == 62 || BitConverter.ToUInt16(header, 18) == 3);
            if (size == 64 && header[0] == 'M' && header[1] == 'Z')
            {
                int pe = BitConverter.ToInt32(header, 0x3c);
                if (pe > 0 && pe + 24 + 2 <= stream.Length)
                {
                    using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
                    stream.Position = pe + 24;
                    ushort magic = reader.ReadUInt16();
                    long cli = pe + 24 + (magic == 0x20b ? 112 : 96) + 14 * 8;
                    if (cli + 4 <= stream.Length) { stream.Position = cli; remove = reader.ReadUInt32() == 0; }
                }
            }
            stream.Close();
            if (remove) File.Delete(path);
        }
        // .NET probes runtimes/<rid>/native before LD_LIBRARY_PATH.
        foreach (string rid in new[] { "linux-x64", "linux-x86", "win-x64", "win-x86", "osx-x64" })
        {
            string native = Path.Combine(gameDir, "runtimes", rid, "native");
            if (Directory.Exists(native)) Directory.Delete(native, recursive: true);
        }
    }
}
