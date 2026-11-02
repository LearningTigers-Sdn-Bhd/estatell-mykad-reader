namespace Estatell.MyKad.Bridge;

/// <summary>Where the log file is. A null <see cref="Path"/> means no file log.</summary>
internal sealed record LogFileOptions(string? Path)
{
    public const string FileName = "estatell-mykad-bridge.log";

    /// <summary>The 1.x path. Other systems get no file log until their item lands.</summary>
    public static string? Default() =>
        OperatingSystem.IsMacOS() ? ForMac(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)) : null;

    public static string ForMac(string home) => System.IO.Path.Combine(home, "Library", "Logs", FileName);
}
