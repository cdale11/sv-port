internal static class ManagedPeTests
{
    internal static void Run(string root)
    {
        string path = Path.Combine(root, "large-managed.dll");
        using (var stream = File.Create(path))
        using (var writer = new BinaryWriter(stream))
        {
            stream.SetLength(64 * 1024 * 1024);
            writer.Write((ushort)0x5a4d);
            stream.Position = 0x3c; writer.Write(0x80);
            stream.Position = 0x84; writer.Write((ushort)0x8664);
            stream.Position = 0x98; writer.Write((ushort)0x20b);
            stream.Position = 0x80 + 24 + 112 + 14 * 8; writer.Write((uint)1);
        }
        ManagedPe.NeedsArchitectureNormalization(path); // warm the method
        long before = GC.GetAllocatedBytesForCurrentThread();
        if (!ManagedPe.NeedsArchitectureNormalization(path)) throw new Exception("Managed AMD64 fixture was not recognized");
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        if (allocated > 128 * 1024) throw new Exception("Architecture check allocated the full DLL: " + allocated);
        using (var stream = File.OpenWrite(path))
        using (var writer = new BinaryWriter(stream))
        {
            stream.Position = 0x84; writer.Write((ushort)0x14c);
        }
        if (ManagedPe.NeedsArchitectureNormalization(path)) throw new Exception("AnyCPU fixture was marked AMD64");
        File.WriteAllBytes(path, new byte[] { 0x4d, 0x5a });
        if (ManagedPe.NeedsArchitectureNormalization(path)) throw new Exception("Truncated fixture was accepted");
        Console.WriteLine("PASS: architecture header scan allocates " + allocated + " bytes for a 64 MiB DLL");
    }
}
