namespace Estatell.MyKad.Bridge;

/// <summary>Where the log file is. A null <see cref="Path"/> means no file log.</summary>
internal sealed record LogFileOptions(string? Path)
{
    public const string FileName = "estatell-mykad-reader.log";

    /// <summary>The user's own log folder. Other systems get no file log.</summary>
    public static string? Default()
    {
        if (OperatingSystem.IsMacOS())
        {
            return ForMac(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        }

        return OperatingSystem.IsWindows() ? ForWindows(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)) : null;
    }

    public static string ForMac(string home) => System.IO.Path.Combine(home, "Library", "Logs", FileName);

    public static string ForWindows(string localAppData) => System.IO.Path.Combine(localAppData, "Estatell", FileName);
}
