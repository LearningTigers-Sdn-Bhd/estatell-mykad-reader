using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using static TrayAot.ObjC;

namespace TrayAot;

/// <summary>An NSStatusItem with a menu. Runs NSApplication on the main thread and never returns.</summary>
[SupportedOSPlatform("macos")]
internal static unsafe class MacTray
{
    private const nint ActivationPolicyAccessory = 1;
    private const double VariableLength = -1;

    private static TrayActions _actions = null!;
    private static nint _header;
    private static nint _menu;
    private static nint _statusItem;

    public static void Run(TrayActions actions, bool selfTest)
    {
        _actions = actions;
        NativeLibrary.Load("/System/Library/Frameworks/AppKit.framework/AppKit");

        var pool = New("NSAutoreleasePool");
        var app = Send(Class("NSApplication"), Sel("sharedApplication"));
        Send(app, Sel("setActivationPolicy:"), ActivationPolicyAccessory);

        var target = Send(Send(CreateTargetClass(), Sel("alloc")), Sel("init"));
        var menu = _menu = New("NSMenu");
        Send(menu, Sel("setDelegate:"), target);
        _header = AddItem(menu, "MyKad Reader spike", 0, 0);
        AddSeparator(menu);
        AddItem(menu, "Open status", Sel("openStatus:"), target);
        AddItem(menu, "Restart server", Sel("restartServer:"), target);
        AddSeparator(menu);
        AddItem(menu, "Quit spike", Sel("quitSpike:"), target);

        // The status bar drops an item nobody retains.
        var statusBar = Send(Class("NSStatusBar"), Sel("systemStatusBar"));
        var statusItem = _statusItem = Send(Send(statusBar, Sel("statusItemWithLength:"), VariableLength), Sel("retain"));
        Send(Send(statusItem, Sel("button")), Sel("setImage:"), LoadTemplateImage("menubar.png"));
        Send(statusItem, Sel("setMenu:"), menu);

        if (selfTest)
        {
            Send(Class("NSTimer"), Sel("scheduledTimerWithTimeInterval:target:selector:userInfo:repeats:"), 0.5, target, Sel("selfTest:"), 0, 0);
        }

        Send(pool, Sel("drain"));
        Send(app, Sel("run"));
    }

    private static nint CreateTargetClass()
    {
        var cls = AllocateClassPair(Class("NSObject"), "TrayAotTarget", 0);
        AddMethod(cls, Sel("openStatus:"), (nint)(delegate* unmanaged<nint, nint, nint, void>)&OpenStatus, "v@:@");
        AddMethod(cls, Sel("restartServer:"), (nint)(delegate* unmanaged<nint, nint, nint, void>)&RestartServer, "v@:@");
        AddMethod(cls, Sel("quitSpike:"), (nint)(delegate* unmanaged<nint, nint, nint, void>)&QuitSpike, "v@:@");
        AddMethod(cls, Sel("menuWillOpen:"), (nint)(delegate* unmanaged<nint, nint, nint, void>)&MenuWillOpen, "v@:@");
        AddMethod(cls, Sel("selfTest:"), (nint)(delegate* unmanaged<nint, nint, nint, void>)&SelfTest, "v@:@");
        RegisterClassPair(cls);
        return cls;
    }

    [UnmanagedCallersOnly]
    private static void OpenStatus(nint self, nint cmd, nint sender) => Guard(_actions.Open);

    [UnmanagedCallersOnly]
    private static void RestartServer(nint self, nint cmd, nint sender) => Guard(_actions.Restart);

    [UnmanagedCallersOnly]
    private static void QuitSpike(nint self, nint cmd, nint sender) => Guard(_actions.Quit);

    [UnmanagedCallersOnly]
    private static void MenuWillOpen(nint self, nint cmd, nint menu) =>
        Guard(() => Send(_header, Sel("setTitle:"), Str($"MyKad Reader spike · {_actions.StatusLine()}")));

    // Without a screen to click: send the Restart item's action and the delegate call through AppKit's own dispatch.
    [UnmanagedCallersOnly]
    private static void SelfTest(nint self, nint cmd, nint timer) => Guard(() =>
    {
        var button = Send(_statusItem, Sel("button"));
        var frame = SendRect(button, Sel("frame"));
        Console.WriteLine($"Self-test: visible {Send(_statusItem, Sel("isVisible")) & 0xFF}, image {Send(button, Sel("image")) != 0}, button {frame.Width}x{frame.Height} pt");
        Send(_menu, Sel("performActionForItemAtIndex:"), (nint)3);
        Send(Send(_menu, Sel("delegate")), Sel("menuWillOpen:"), _menu);
        Console.WriteLine($"Self-test: header \"{ToStr(Send(_header, Sel("title")))}\"");
    });

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
    private static nint LoadTemplateImage(string resource)
    {
        var bytes = Resources.Read(resource);
        nint data;
        fixed (byte* pointer = bytes)
        {
            data = Send(Class("NSData"), Sel("dataWithBytes:length:"), (nint)pointer, (nuint)bytes.Length);
        }

        var image = Send(Send(Class("NSImage"), Sel("alloc")), Sel("initWithData:"), data);
        Send(image, Sel("setSize:"), new NSSize(18, 18));
        Send(image, Sel("setTemplate:"), (nint)1);
        return image;
    }
}
