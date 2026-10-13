internal static class ManagedPe
{
    // Architecture checks need only PE/CLI headers, not the full managed DLL.
    internal static bool NeedsArchitectureNormalization(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);
        if (stream.Length < 0x40 || reader.ReadUInt16() != 0x5a4d) return false;
        stream.Position = 0x3c;
        int pe = reader.ReadInt32();
        if (pe <= 0 || (long)pe + 160 >= stream.Length) return false;
        stream.Position = (long)pe + 4;
        ushort machine = reader.ReadUInt16();
        stream.Position = (long)pe + 24;
        ushort magic = reader.ReadUInt16();
        if (magic != 0x10b && magic != 0x20b) return false;
        long cli = (long)pe + 24 + (magic == 0x20b ? 112 : 96) + 14 * 8;
        if (cli + 4 > stream.Length) return false;
        stream.Position = cli;
        return machine == 0x8664 && reader.ReadUInt32() != 0;
    }
}
