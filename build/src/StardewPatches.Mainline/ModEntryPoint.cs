using System;
using System.IO;
using System.Runtime;
using System.Reflection;
using System.Threading;

// A runtime startup hook runs before the retail entrypoint, unlike the patch
// entrypoint which is reached only during MonoGame initialization.
internal static class StartupHook
{
    public static void Initialize()
    {
        int workers, io;
        ThreadPool.GetMinThreads(out workers, out io);
        ThreadPool.SetMinThreads(Math.Max(workers, Environment.ProcessorCount), io);
        ThreadPool.GetMinThreads(out workers, out io);
        if (Environment.GetEnvironmentVariable("SDV_STARTUP_DIAGNOSTICS") != "1") return;
        Console.WriteLine($"Managed startup at {DateTimeOffset.Now:O}; CPUs={Environment.ProcessorCount}; GC={(GCSettings.IsServerGC ? "server" : "workstation")}; worker minimum={workers}; PGO={Environment.GetEnvironmentVariable("DOTNET_TieredPGO")}");
        Console.WriteLine("Entry assembly: " + Assembly.GetEntryAssembly()?.GetName());
        Console.WriteLine("Runtime tiered config=" + AppContext.GetData("System.Runtime.TieredCompilation") +
            "; quick-loops config=" + AppContext.GetData("System.Runtime.TieredCompilation.QuickJitForLoops"));
        if (File.Exists("/proc/self/status"))
            foreach (string line in File.ReadLines("/proc/self/status"))
                if (line.StartsWith("Cpus_allowed_list:", StringComparison.Ordinal)) Console.WriteLine(line);
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class ModEntryPointAttribute : Attribute
{
}

[ModEntryPoint]
public static class StardewPatches
{
    public static void Main()
    {
        Console.Out.WriteLine($"StardewPatches initialized at {DateTimeOffset.Now:O}");
    }
}
