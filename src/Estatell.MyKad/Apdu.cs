namespace Estatell.MyKad;

/// <summary>The commands of the JPN read sequence, byte for byte as in the Python bridge.</summary>
internal static class Apdu
{
    /// <summary>The most a single read command asks for.</summary>
    public const int MaxChunk = 255;

    public static readonly byte[] SelectJpn = [0x00, 0xA4, 0x04, 0x00, 0x0A, 0xA0, 0x00, 0x00, 0x00, 0x74, 0x4A, 0x50, 0x4E, 0x00, 0x10];

    public static readonly byte[] SelectJpnAccept = [0x90, 0x61];
    public static readonly byte[] SetLengthAccept = [0x90, 0x91];
    public static readonly byte[] FileAccept = [0x90, 0x91, 0x94];

    public static byte[] SetLength(ushort length) =>
        [0xC8, 0x32, 0x00, 0x00, 0x05, 0x08, 0x00, 0x00, (byte)length, (byte)(length >> 8)];

    public static byte[] SelectFile(JpnFile file) =>
        [
            0xCC, 0x00, 0x00, 0x00, 0x08,
            (byte)file.File2, (byte)(file.File2 >> 8),
            (byte)file.File1, (byte)(file.File1 >> 8),
            0x00, 0x00, // offset
            (byte)file.Length, (byte)(file.Length >> 8),
        ];

    public static byte[] ReadChunk(byte length) => [0xCC, 0x06, 0x00, 0x00, length];
}

/// <summary>A file in the JPN application.</summary>
internal sealed record JpnFile(string Name, ushort File1, ushort File2, ushort Length)
{
    public static readonly JpnFile Identity = new("JPN1-1", 1, 1, 459);
    public static readonly JpnFile Address = new("JPN1-4", 1, 4, 171);
    public static readonly JpnFile Portrait = new("JPN1-2", 1, 2, 4011);
}
