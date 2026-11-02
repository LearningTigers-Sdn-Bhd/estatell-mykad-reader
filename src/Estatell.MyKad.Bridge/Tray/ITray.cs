using System.ComponentModel;
using System.Diagnostics;
using Estatell.MyKad.Reader;

namespace Estatell.MyKad.Bridge.Tray;

/// <summary>The icon and menu. See docs/02-bridge/04-tray.md.</summary>
/// <remarks>
/// No Dispose: a restart disposes the app that created the tray, and the tray must outlive it. <see cref="Run"/> removes the icon when it ends.
/// </remarks>
internal interface ITray
{
    /// <summary>Shows the icon and blocks until <see cref="Stop"/>. Call on the main thread, because AppKit must run on it.</summary>
    void Run(TrayActions actions);

    /// <summary>Makes <see cref="Run"/> return. Safe from any thread. A stop before the run makes the run return at once.</summary>
    void Stop();
}

/// <summary>What the menu can do. Safe to call on the tray's UI thread: none of these wait for the server.</summary>
internal sealed record TrayActions(Action Help, Action Restart, Func<TrayStatus> Status);

/// <summary>The lines of the menu. Never holds card data.</summary>
/// <param name="ReaderName">Shown on the card reader line. Null shows "not found".</param>
/// <param name="EstatellHost">Shown on the Estatell line. Null shows "not seen".</param>
internal sealed record TrayStatus(TrayState State, string? ReaderName, bool CardPresent, string? EstatellHost)
{
    public static readonly TrayStatus NoReader = new(TrayState.NoReader, null, false, null);

    public static TrayStatus From(BridgeStatus status, string? estatellHost, bool waitingForPort) =>
        new(
            waitingForPort ? TrayState.OtherUser : status.ReaderConnected ? TrayState.Ready : TrayState.NoReader,
            status.ReaderName,
            status.CardPresent,
            estatellHost);

    public static TrayStatus Read(IReader reader, ILogger<BridgeStatus> logger, EstatellPresence presence, bool waitingForPort) =>
        From(StatusRoute.Get(reader, logger), presence.ConnectedHost, waitingForPort);
}

/// <summary>The status line and the icon. See the table in docs/02-bridge/04-tray.md#menu.</summary>
internal enum TrayState
{
    Ready,
    NoReader,
    OtherUser,
}

/// <summary>The server without an icon, for <c>--no-tray</c> and the tests.</summary>
internal sealed class NoTray : ITray
{
    private readonly TaskCompletionSource _stopped = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void Run(TrayActions actions) => _stopped.Task.Wait();

    public void Stop() => _stopped.TrySetResult();
}

internal static class TrayChooser
{
    /// <summary>The Windows tray adds its branch here when its item lands.</summary>
    public static ITray Create(BridgeOptions options)
    {
        if (options.NoTray)
        {
            return new NoTray();
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MacTray();
        }

        return new NoTray();
    }
}

internal static partial class TrayHelp
{
    /// <summary>Opens the set-up page in the default browser. A failure only goes to the log, because the tray has no window to show it in.</summary>
    public static void Open(ILogger logger)
    {
        try
        {
            Process.Start(new ProcessStartInfo(BridgeInfo.HelpUrl) { UseShellExecute = true })?.Dispose();
        }
        catch (Exception e) when (e is Win32Exception or InvalidOperationException)
        {
            LogHelpFailed(logger, e);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not open the help page.")]
    private static partial void LogHelpFailed(ILogger logger, Exception exception);
}
