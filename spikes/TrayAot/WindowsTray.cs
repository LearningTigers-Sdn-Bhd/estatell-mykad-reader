using System.Buffers.Binary;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;
using static TrayAot.Win32;

namespace TrayAot;

/// <summary>
/// A notification area icon with a menu, on its own thread with a message loop.
/// The window is a hidden top-level window, not a message-only one: message-only windows get no broadcasts,
/// so they would miss TaskbarCreated (Explorer restarted) and WM_SETTINGCHANGE (taskbar theme changed).
/// </summary>
[SupportedOSPlatform("windows")]
internal static unsafe class WindowsTray
{
    private const string ClassName = "EstatellTrayAotSpike";
    private const uint TrayMessage = WmApp + 1;
    private const int OpenId = 1;
    private const int RestartId = 2;
    private const int QuitId = 3;

    private static TrayActions _actions = null!;
    private static nint _window;
    private static nint _icon;
    private static uint _taskbarCreated;

    public static void Run(TrayActions actions)
    {
        _actions = actions;
        var thread = new Thread(MessageLoop) { Name = "Tray" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }

    private static void MessageLoop()
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
        _window = CreateWindowEx(0, ClassName, "MyKad Reader spike", WsPopup, 0, 0, 0, 0, 0, 0, instance, 0);
        if (_window == 0)
        {
            throw new Win32Exception();
        }

        _icon = LoadIcon();
        Notify(NimAdd);

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
            if (message == TrayMessage)
            {
                if ((uint)(lParam & 0xFFFF) is WmRButtonUp or WmLButtonUp)
                {
                    ShowMenu();
                }

                return 0;
            }

            if (message == _taskbarCreated && _taskbarCreated != 0)
            {
                Notify(NimAdd);
                return 0;
            }

            if (message == WmSettingChange && lParam != 0 && new string((char*)lParam) == "ImmersiveColorSet")
            {
                var old = _icon;
                _icon = LoadIcon();
                Notify(NimModify);
                _ = DestroyIcon(old);
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

    private static void ShowMenu()
    {
        var menu = CreatePopupMenu();
        AppendMenu(menu, MfString | MfGrayed, 0, $"MyKad Reader spike · {_actions.StatusLine()}");
        AppendMenu(menu, MfSeparator, 0, null);
        AppendMenu(menu, MfString, OpenId, "Open status");
        AppendMenu(menu, MfString, RestartId, "Restart server");
        AppendMenu(menu, MfSeparator, 0, null);
        AppendMenu(menu, MfString, QuitId, "Quit spike");

        // Without SetForegroundWindow the menu does not close when the user clicks elsewhere.
        Point point;
        _ = GetCursorPos(&point);
        _ = SetForegroundWindow(_window);
        var id = TrackPopupMenu(menu, TpmRightButton | TpmReturnCmd | TpmNoNotify, point.X, point.Y, 0, _window, 0);
        _ = PostMessage(_window, WmNull, 0, 0);
        _ = DestroyMenu(menu);

        switch (id)
        {
            case OpenId:
                _actions.Open();
                break;
            case RestartId:
                _actions.Restart();
                break;
            case QuitId:
                Notify(NimDelete);
                _actions.Quit();
                break;
        }
    }

    private static void Notify(uint message)
    {
        var data = new NotifyIconData
        {
            Size = (uint)sizeof(NotifyIconData),
            Window = _window,
            Id = 1,
            Flags = NifMessage | NifIcon | NifTip,
            CallbackMessage = TrayMessage,
            Icon = _icon,
        };
        "MyKad Reader spike".AsSpan().CopyTo(new Span<char>(data.Tip, 127));
        if (ShellNotifyIcon(message, &data) == 0)
        {
            Console.Error.WriteLine($"Shell_NotifyIcon({message}) failed");
        }
    }

    // A dark glyph on a light taskbar, a white glyph on a dark one.
    private static nint LoadIcon()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        var light = key?.GetValue("SystemUsesLightTheme") is int value && value == 1;
        var ico = Resources.Read(light ? "icon-on-light.ico" : "icon-on-dark.ico");

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
