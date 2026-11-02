namespace Estatell.MyKad.Bridge.Tray;

/// <summary>The words of the macOS menu. See the table in docs/02-bridge/04-tray.md#menu. Never holds card data.</summary>
internal static class MacMenuText
{
    public static string Header => $"MyKad Reader {BridgeInfo.Version}";

    public static string StatusLine(TrayState state) => state switch
    {
        TrayState.Ready => "● Ready",
        TrayState.NoReader => "○ Plug in the card reader",
        TrayState.OtherUser => "○ MyKad Reader is open for another user",
        _ => throw new ArgumentOutOfRangeException(nameof(state)),
    };

    public static string ReaderLine(TrayStatus status) => $"Card reader: {status.ReaderName ?? "not found"}";

    public static string CardLine(TrayStatus status) => $"Card: {(status.CardPresent ? "inserted" : "none")}";

    public static string EstatellLine(TrayStatus status) =>
        status.EstatellHost is { } host ? $"Estatell: connected · {host}" : "Estatell: not seen";

    /// <summary>Grey when the reader is not usable. "not seen" does not change the icon.</summary>
    public static bool IconGrey(TrayState state) => state != TrayState.Ready;
}
