namespace TrayAot;

internal static class Resources
{
    public static byte[] Read(string name)
    {
        using var stream = typeof(Resources).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"No embedded resource {name}");
        var bytes = new byte[stream.Length];
        stream.ReadExactly(bytes);
        return bytes;
    }
}
