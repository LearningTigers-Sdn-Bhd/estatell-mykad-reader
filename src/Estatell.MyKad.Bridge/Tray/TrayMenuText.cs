namespace Estatell.MyKad.Bridge.Tray;

/// <summary>The words of the tray menu, shared by the macOS and Windows trays. See the table in docs/02-bridge/04-tray.md#menu. Never holds card data.</summary>
internal static class TrayMenuText
{
    private const string ReaderSuffix = " ICC Reader";

    public const string Help = "Set-up help";

    public const string Restart = "Restart MyKad Reader";

    public static string Header => $"Estatell MyKad Reader {BridgeInfo.Version}";

    /// <summary>The one line that says what to do next. A card in the reader only counts when the reader is ready.</summary>
    public static string MainLine(TrayStatus status) => $"{(status.State == TrayState.Ready ? "🟢" : "⚪")} {MainWords(status)}";

    /// <summary>The main line without its dot, for Windows: its menus draw an emoji without colour, so a green dot looks grey.</summary>
    public static string MainWords(TrayStatus status) => status.State switch
    {
        TrayState.Ready => status.CardPresent ? "MyKad inserted" : "Ready. Insert a MyKad",
        TrayState.NoReader => "Plug in the card reader",
        TrayState.OtherUser => "Open for another user on this computer",
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    public static string ReaderLine(TrayStatus status) => $"Card reader: {ShortReaderName(status.ReaderName) ?? "not found"}";

    public static string EstatellLine(TrayStatus status) =>
        status.EstatellHost is null ? "Estatell: no page open" : "Estatell: page open";

    /// <summary>Cuts everything from " ICC Reader" onward, so "ACS ACR39U ICC Reader 0" shows as "ACS ACR39U". Null for a missing or empty name.</summary>
    public static string? ShortReaderName(string? name)
    {
        if (name is null)
        {
            return null;
        }

        var cut = name.IndexOf(ReaderSuffix, StringComparison.Ordinal);
        var shortName = (cut >= 0 ? name[..cut] : name).Trim();
        return shortName.Length == 0 ? null : shortName;
    }

    /// <summary>Grey when the reader is not usable. "no page open" does not change the icon.</summary>
    public static bool IconGrey(TrayState state) => state != TrayState.Ready;
}
