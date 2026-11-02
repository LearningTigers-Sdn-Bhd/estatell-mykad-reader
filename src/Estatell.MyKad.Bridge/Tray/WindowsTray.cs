using System.Buffers.Binary;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using static Estatell.MyKad.Bridge.Tray.Win32;

namespace Estatell.MyKad.Bridge.Tray;

/// <summary>
/// A notification area icon with a menu, on its own thread with a message loop. See docs/02-bridge/04-tray.md#windows.
/// The window is a hidden top-level window, not a message-only one: those get no broadcasts, so they would miss TaskbarCreated (Explorer restarted).
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed unsafe class WindowsTray : ITray
{
    private const string ClassName = "EstatellMyKadTray";
    private const string Tip = "MyKad Reader";
    private const uint TrayMessage = WmApp + 1;
    private const int HelpId = 1;
    private const int RestartId = 2;
    private const nuint RefreshTimerId = 1;

    // Plugging a reader in or out has no callback here, so the icon follows the status on a timer.
    private const uint IconRefreshMilliseconds = 2000;

    // Windows calls back through a static function, and there is one tray per process.
    private static WindowsTray? s_current;

    private readonly object _lock = new();
    private bool _stopRequested;
    private nint _window;

    private TrayActions _actions = null!;
    private nint _readyIcon;
    private nint _greyIcon;
    private bool _grey;
    private uint _taskbarCreated;

    public void Run(TrayActions actions)
    {
        _actions = actions;
        s_current = this;
        var thread = new Thread(MessageLoop) { Name = "Tray" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }

    public void Stop()
    {
        lock (_lock)
        {
            _stopRequested = true;
            if (_window != 0)
            {
                _ = PostMessage(_window, WmClose, 0, 0);
            }
        }
    }

    private void MessageLoop()
    {
        _ = SetProcessDpiAwarenessContext(DpiAwarenessPerMonitorV2);
        var instance = GetModuleHandle(null);
        fixed (char* className = ClassName)
        {
            var windowClass = new WndClassEx
            {
                Size = (uint)sizeof(WndClassEx),
                WndProc = &WindowProc,
                Instance = instance,
                ClassName = className,
            };
            if (RegisterClassEx(&windowClass) == 0)
            {
                throw new Win32Exception();
            }
        }

        _taskbarCreated = RegisterWindowMessage("TaskbarCreated");
        _readyIcon = LoadIcon("tray-ready.ico");
        _greyIcon = LoadIcon("tray-grey.ico");

        var window = CreateWindowEx(0, ClassName, Tip, WsPopup, 0, 0, 0, 0, 0, 0, instance, 0);
        if (window == 0)
        {
            throw new Win32Exception();
        }

        lock (_lock)
        {
            _window = window;
            if (_stopRequested)
            {
                return;
            }
        }

        _grey = TrayMenuText.IconGrey(_actions.Status().State);
        Notify(NimAdd);
        _ = SetTimer(window, RefreshTimerId, IconRefreshMilliseconds, 0);

        Msg message;
        while (GetMessage(&message, 0, 0, 0) > 0)
        {
            _ = TranslateMessage(&message);
            DispatchMessage(&message);
        }
    }

    [UnmanagedCallersOnly]
    private static nint WindowProc(nint window, uint message, nint wParam, nint lParam)
    {
        try
        {
            if (s_current?.Handle(message, lParam) == true)
            {
                return 0;
            }
        }
        catch (Exception e)
        {
            // An exception must not cross back into user32.
            Console.Error.WriteLine(e);
        }

        return DefWindowProc(window, message, wParam, lParam);
    }

    private bool Handle(uint message, nint lParam)
    {
        switch (message)
        {
            case TrayMessage:
                if ((uint)(lParam & 0xFFFF) is WmRButtonUp or WmLButtonUp)
                {
                    ShowMenu();
                }

                return true;
            case WmTimer:
                RefreshIcon();
                return true;
            case WmDestroy:
                Notify(NimDelete);
                PostQuitMessage(0);
                return true;
            default:
                if (message == _taskbarCreated && _taskbarCreated != 0)
                {
                    Notify(NimAdd);
                    return true;
                }

                return false;
        }
    }

    private void ShowMenu()
    {
        var status = _actions.Status();
        RefreshIcon(status);

        var menu = CreatePopupMenu();
        _ = AppendMenu(menu, MfString | MfDisabled, 0, TrayMenuText.Header);
        _ = AppendMenu(menu, MfString | MfDisabled, 0, TrayMenuText.MainLine(status));
        _ = AppendMenu(menu, MfSeparator, 0, null);
        _ = AppendMenu(menu, MfString | MfDisabled, 0, TrayMenuText.ReaderLine(status));
        _ = AppendMenu(menu, MfString | MfDisabled, 0, TrayMenuText.EstatellLine(status));
        _ = AppendMenu(menu, MfSeparator, 0, null);
        _ = AppendMenu(menu, MfString, HelpId, TrayMenuText.Help);
        _ = AppendMenu(menu, MfString, RestartId, TrayMenuText.Restart);

        // Without SetForegroundWindow the menu does not close when the user clicks elsewhere.
        Point point;
        _ = GetCursorPos(&point);
        _ = SetForegroundWindow(_window);
        var id = TrackPopupMenu(menu, TpmRightButton | TpmReturnCmd | TpmNoNotify, point.X, point.Y, 0, _window, 0);
        _ = PostMessage(_window, WmNull, 0, 0);
        _ = DestroyMenu(menu);

        switch (id)
        {
            case HelpId:
                _actions.Help();
                break;
            case RestartId:
                _actions.Restart();
                break;
        }
    }

    private void RefreshIcon() => RefreshIcon(_actions.Status());

    private void RefreshIcon(TrayStatus status)
    {
        var grey = TrayMenuText.IconGrey(status.State);
        if (_grey != grey)
        {
            _grey = grey;
            Notify(NimModify);
        }
    }

    private void Notify(uint message)
    {
        var data = new NotifyIconData
        {
            Size = (uint)sizeof(NotifyIconData),
            Window = _window,
            Id = 1,
            Flags = NifMessage | NifIcon | NifTip,
            CallbackMessage = TrayMessage,
            Icon = _grey ? _greyIcon : _readyIcon,
        };
        Tip.AsSpan().CopyTo(new Span<char>(data.Tip, 127));
        _ = ShellNotifyIcon(message, &data);
    }

    private static nint LoadIcon(string name)
    {
        using var stream = typeof(WindowsTray).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Missing resource {name}.");
        var ico = new byte[stream.Length];
        stream.ReadExactly(ico);

        // Pick the smallest image at least as big as a small icon at this DPI, else the biggest.
        var want = GetSystemMetrics(SmCxSmIcon);
        int count = BinaryPrimitives.ReadUInt16LittleEndian(ico.AsSpan(4));
        int best = -1, bestSize = 0;
        for (var i = 0; i < count; i++)
        {
            var size = ico[6 + (16 * i)] is 0 ? 256 : ico[6 + (16 * i)];
            var better = best < 0
                || (size >= want && (bestSize < want || size < bestSize))
                || (size < want && bestSize < want && size > bestSize);
            if (better)
            {
                best = i;
                bestSize = size;
            }
        }

        var entry = ico.AsSpan(6 + (16 * best));
        var length = BinaryPrimitives.ReadUInt32LittleEndian(entry[8..]);
        var offset = (int)BinaryPrimitives.ReadUInt32LittleEndian(entry[12..]);
        fixed (byte* bits = &ico[offset])
        {
            var icon = CreateIconFromResourceEx(bits, length, 1, 0x00030000, want, want, 0);
            return icon != 0 ? icon : throw new Win32Exception();
        }
    }
}
