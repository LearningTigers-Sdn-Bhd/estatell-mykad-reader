using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using static Estatell.MyKad.Bridge.Tray.ObjC;

namespace Estatell.MyKad.Bridge.Tray;

/// <summary>An NSStatusItem with a menu. Runs NSApplication on the main thread. See docs/02-bridge/04-tray.md#macos.</summary>
[SupportedOSPlatform("macos")]
internal sealed unsafe class MacTray : ITray
{
    private const nint ActivationPolicyAccessory = 1;
    private const nint ApplicationDefinedEvent = 15;
    private const double VariableLength = -1;

    // Plugging a reader in or out has no callback here, so the icon follows the status on a timer.
    private const double IconRefreshSeconds = 2;

    // AppKit calls back through static functions, and there is one tray per process.
    private static MacTray? s_current;

    private readonly object _lock = new();
    private bool _stopRequested;
    private nint _target;

    private TrayActions _actions = null!;
    private nint _readerItem;
    private nint _cardItem;
    private nint _estatellItem;
    private nint _statusLineItem;
    private nint _statusButton;
    private nint _readyImage;
    private bool? _grey;

    public void Run(TrayActions actions)
    {
        lock (_lock)
        {
            if (_stopRequested)
            {
                return;
            }
        }

        _actions = actions;
        s_current = this;
        NativeLibrary.Load("/System/Library/Frameworks/AppKit.framework/AppKit");

        var pool = New("NSAutoreleasePool");
        var app = Send(Class("NSApplication"), Sel("sharedApplication"));
        Send(app, Sel("setActivationPolicy:"), ActivationPolicyAccessory);

        var target = Send(Send(CreateTargetClass(), Sel("alloc")), Sel("init"));
        var menu = New("NSMenu");
        Send(menu, Sel("setDelegate:"), target);
        AddItem(menu, MacMenuText.Header, 0, 0);
        _statusLineItem = AddItem(menu, "", 0, 0);
        AddSeparator(menu);
        _readerItem = AddItem(menu, "", 0, 0);
        _cardItem = AddItem(menu, "", 0, 0);
        _estatellItem = AddItem(menu, "", 0, 0);
        AddSeparator(menu);
        AddItem(menu, "Help", Sel("help:"), target);
        AddItem(menu, "Restart MyKad Reader", Sel("restart:"), target);

        // The status bar drops an item nobody retains.
        var statusBar = Send(Class("NSStatusBar"), Sel("systemStatusBar"));
        var statusItem = Send(Send(statusBar, Sel("statusItemWithLength:"), VariableLength), Sel("retain"));
        _statusButton = Send(statusItem, Sel("button"));
        _readyImage = LoadTemplateImage();
        Send(_statusButton, Sel("setImage:"), _readyImage);
        Send(statusItem, Sel("setMenu:"), menu);
        Refresh();

        Send(Class("NSTimer"), Sel("scheduledTimerWithTimeInterval:target:selector:userInfo:repeats:"), IconRefreshSeconds, target, Sel("refresh:"), 0, 1);
        Send(pool, Sel("drain"));

        lock (_lock)
        {
            _target = target;
            if (_stopRequested)
            {
                _target = 0;
            }
        }

        if (_target != 0)
        {
            Send(app, Sel("run"));
        }

        Send(statusBar, Sel("removeStatusItem:"), statusItem);
        s_current = null;
    }

    public void Stop()
    {
        nint target;
        lock (_lock)
        {
            _stopRequested = true;
            target = _target;
        }

        if (target != 0)
        {
            // AppKit objects belong to the main thread.
            Send(target, Sel("performSelectorOnMainThread:withObject:waitUntilDone:"), Sel("stop:"), 0, 0);
        }
    }

    private static nint CreateTargetClass()
    {
        var cls = AllocateClassPair(Class("NSObject"), "EstatellMyKadTrayTarget", 0);
        AddMethod(cls, Sel("help:"), (nint)(delegate* unmanaged<nint, nint, nint, void>)&OnHelp, "v@:@");
        AddMethod(cls, Sel("restart:"), (nint)(delegate* unmanaged<nint, nint, nint, void>)&OnRestart, "v@:@");
        AddMethod(cls, Sel("menuWillOpen:"), (nint)(delegate* unmanaged<nint, nint, nint, void>)&OnMenuWillOpen, "v@:@");
        AddMethod(cls, Sel("refresh:"), (nint)(delegate* unmanaged<nint, nint, nint, void>)&OnRefresh, "v@:@");
        AddMethod(cls, Sel("stop:"), (nint)(delegate* unmanaged<nint, nint, nint, void>)&OnStop, "v@:@");
        RegisterClassPair(cls);
        return cls;
    }

    [UnmanagedCallersOnly]
    private static void OnHelp(nint self, nint cmd, nint sender) => Guard(() => s_current?._actions.Help());

    [UnmanagedCallersOnly]
    private static void OnRestart(nint self, nint cmd, nint sender) => Guard(() => s_current?._actions.Restart());

    [UnmanagedCallersOnly]
    private static void OnMenuWillOpen(nint self, nint cmd, nint menu) => Guard(() => s_current?.Refresh());

    [UnmanagedCallersOnly]
    private static void OnRefresh(nint self, nint cmd, nint timer) => Guard(() => s_current?.Refresh());

    [UnmanagedCallersOnly]
    private static void OnStop(nint self, nint cmd, nint sender) => Guard(() =>
    {
        var app = Send(Class("NSApplication"), Sel("sharedApplication"));
        Send(app, Sel("stop:"), 0);

        // stop: takes effect after the next event, and a call from outside an event leaves the loop waiting for one.
        var wake = SendEvent(Class("NSEvent"), Sel("otherEventWithType:location:modifierFlags:timestamp:windowNumber:context:subtype:data1:data2:"), ApplicationDefinedEvent, default, 0, 0, 0, 0, 0, 0, 0);
        Send(app, Sel("postEvent:atStart:"), wake, 1);
    });

    // Titles and icon from the current status.
    private void Refresh()
    {
        var status = _actions.Status();
        SetTitle(_statusLineItem, MacMenuText.StatusLine(status.State));
        SetTitle(_readerItem, MacMenuText.ReaderLine(status));
        SetTitle(_cardItem, MacMenuText.CardLine(status));
        SetTitle(_estatellItem, MacMenuText.EstatellLine(status));

        var grey = MacMenuText.IconGrey(status.State);
        if (_grey != grey)
        {
            _grey = grey;
            Send(_statusButton, Sel("setAppearsDisabled:"), grey ? 1 : 0);
        }
    }

    private static void SetTitle(nint item, string title) => Send(item, Sel("setTitle:"), Str(title));

    // An exception must not cross back into Objective-C.
    private static void Guard(Action action)
    {
        try
        {
            action();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }
    }

    // An item without an action is greyed out, because NSMenu enables items by their action.
    private static nint AddItem(nint menu, string title, nint action, nint target)
    {
        var item = Send(Send(Class("NSMenuItem"), Sel("alloc")), Sel("initWithTitle:action:keyEquivalent:"), Str(title), action, Str(""));
        if (target != 0)
        {
            Send(item, Sel("setTarget:"), target);
        }

        Send(menu, Sel("addItem:"), item);
        return item;
    }

    private static void AddSeparator(nint menu) => Send(menu, Sel("addItem:"), Send(Class("NSMenuItem"), Sel("separatorItem")));

    // 36 px drawn at 18 pt, so it is sharp on Retina. A template image is coloured by macOS for light and dark menu bars.
    private static nint LoadTemplateImage()
    {
        using var stream = typeof(MacTray).Assembly.GetManifestResourceStream("menubar.png")
            ?? throw new InvalidOperationException("No embedded resource menubar.png");
        var bytes = new byte[stream.Length];
        stream.ReadExactly(bytes);

        nint data;
        fixed (byte* pointer = bytes)
        {
            data = Send(Class("NSData"), Sel("dataWithBytes:length:"), (nint)pointer, (nuint)bytes.Length);
        }

        var image = Send(Send(Class("NSImage"), Sel("alloc")), Sel("initWithData:"), data);
        Send(image, Sel("setSize:"), new NSSize(18, 18));
        Send(image, Sel("setTemplate:"), 1);
        return image;
    }
}
