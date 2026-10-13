using Mono.Cecil;

// Cecil's resolver owns cached dependency assemblies separately from the
// assembly being edited. Both must close before copying overlays or SMAPI files.
internal sealed class AssemblyReadScope : IDisposable
{
    private readonly DefaultAssemblyResolver resolver = new();
    internal AssemblyDefinition Assembly { get; }

    internal AssemblyReadScope(string assemblyPath, string searchDirectory)
    {
        resolver.AddSearchDirectory(searchDirectory);
        try
        {
            Assembly = AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters
            {
                InMemory = true,
                ReadSymbols = false,
                AssemblyResolver = resolver,
            });
        }
        catch
        {
            resolver.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        try { Assembly.Dispose(); }
        finally { resolver.Dispose(); }
    }
}
