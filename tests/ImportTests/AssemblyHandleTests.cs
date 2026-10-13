using Mono.Cecil;

internal static class AssemblyHandleTests
{
    internal static void Run(string root)
    {
        string gameDir = Path.Combine(root, "assembly-handles");
        string overlayDir = Path.Combine(root, "overlay");
        Directory.CreateDirectory(gameDir);
        Directory.CreateDirectory(overlayDir);
        string dependencyPath = Path.Combine(gameDir, "MonoGame.Framework.dll");
        string gamePath = Path.Combine(gameDir, "Stardew Valley.dll");
        using (var dependency = AssemblyDefinition.CreateAssembly(
            new AssemblyNameDefinition("MonoGame.Framework", new Version(1, 0)), "MonoGame.Framework", ModuleKind.Dll))
        {
            dependency.MainModule.Types.Add(new TypeDefinition("Fixture", "FrameworkType",
                TypeAttributes.Public | TypeAttributes.Class, dependency.MainModule.TypeSystem.Object));
            dependency.Write(dependencyPath);
        }
        using (var game = AssemblyDefinition.CreateAssembly(
            new AssemblyNameDefinition("Stardew Valley", new Version(1, 0)), "Stardew Valley", ModuleKind.Dll))
        {
            game.MainModule.AssemblyReferences.Add(new AssemblyNameReference("MonoGame.Framework", new Version(1, 0)));
            game.Write(gamePath);
        }
        File.WriteAllText(Path.Combine(overlayDir, "MonoGame.Framework.dll"), "updated framework");

        foreach (bool failRewrite in new[] { false, true })
        {
            // Restore the real dependency between passes.
            string backup = dependencyPath + ".original";
            if (!File.Exists(backup)) File.Copy(dependencyPath, backup);
            else File.Copy(backup, dependencyPath, overwrite: true);
            try
            {
                using var scope = new AssemblyReadScope(gamePath, gameDir);
                var reference = new TypeReference("Fixture", "FrameworkType", scope.Assembly.MainModule,
                    scope.Assembly.MainModule.AssemblyReferences.Single(r => r.Name == "MonoGame.Framework"));
                if (reference.Resolve() is null) throw new Exception("Dependency fixture did not resolve");
                // Prove this fixture exercises a real held file handle.
                bool locked = false;
                try { using var writer = new FileStream(dependencyPath, FileMode.Open, FileAccess.Write, FileShare.None); }
                catch (IOException) { locked = true; }
                if (!locked) throw new Exception("Dependency fixture did not acquire an exclusive-copy conflict");
                if (failRewrite) throw new InvalidDataException("Synthetic rewrite failure");
            }
            catch (InvalidDataException) when (failRewrite) { }

            // This is the same overlay operation that failed on the handheld.
            GameImport.CopyOverlayFiles(overlayDir, gameDir);
            if (File.ReadAllText(dependencyPath) != "updated framework") throw new Exception("Overlay copy failed after resolver disposal");
        }
        Console.WriteLine("PASS: Cecil dependency handles close before overlay copy, including failed rewrites");
    }
}
