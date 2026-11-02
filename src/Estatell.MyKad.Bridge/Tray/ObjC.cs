using System.Runtime.InteropServices;

namespace Estatell.MyKad.Bridge.Tray;

/// <summary>The Objective-C runtime calls the macOS tray needs. objc_msgSend gets one typed import per signature.</summary>
internal static partial class ObjC
{
    private const string Lib = "/usr/lib/libobjc.A.dylib";

    [LibraryImport(Lib, EntryPoint = "objc_getClass", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint GetClass(string name);

    [LibraryImport(Lib, EntryPoint = "sel_registerName", StringMarshalling = StringMarshalling.Utf8)]
    public static partial nint Sel(string name);

    [LibraryImport(Lib, EntryPoint = "objc_allocateClassPair", StringMarshalling = StringMarshalling.Utf8)]
    public static partial nint AllocateClassPair(nint superclass, string name, nuint extraBytes);

    [LibraryImport(Lib, EntryPoint = "objc_registerClassPair")]
    public static partial void RegisterClassPair(nint cls);

    [LibraryImport(Lib, EntryPoint = "class_addMethod", StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool AddMethod(nint cls, nint sel, nint imp, string types);

    [LibraryImport(Lib, EntryPoint = "objc_msgSend")]
    public static partial nint Send(nint receiver, nint sel);

    [LibraryImport(Lib, EntryPoint = "objc_msgSend")]
    public static partial nint Send(nint receiver, nint sel, nint arg);

    [LibraryImport(Lib, EntryPoint = "objc_msgSend")]
    public static partial nint Send(nint receiver, nint sel, nint arg, nuint length);

    [LibraryImport(Lib, EntryPoint = "objc_msgSend")]
    public static partial nint Send(nint receiver, nint sel, nint arg1, nint arg2, nint arg3);

    [LibraryImport(Lib, EntryPoint = "objc_msgSend")]
    public static partial nint Send(nint receiver, nint sel, double arg);

    [LibraryImport(Lib, EntryPoint = "objc_msgSend")]
    public static partial nint Send(nint receiver, nint sel, double interval, nint target, nint selector, nint userInfo, nint repeats);

    [LibraryImport(Lib, EntryPoint = "objc_msgSend")]
    public static partial void Send(nint receiver, nint sel, NSSize arg);

    [LibraryImport(Lib, EntryPoint = "objc_msgSend")]
    public static partial nint SendEvent(nint receiver, nint sel, nint type, NSPoint location, nuint flags, double timestamp, nint window, nint context, short subtype, nint data1, nint data2);

    public static nint Class(string name)
    {
        var cls = GetClass(name);
        return cls != 0 ? cls : throw new InvalidOperationException($"No Objective-C class {name}");
    }

    public static nint New(string className) => Send(Send(Class(className), Sel("alloc")), Sel("init"));

    /// <summary>An autoreleased NSString.</summary>
    public static nint Str(string value)
    {
        var utf8 = Marshal.StringToCoTaskMemUTF8(value);
        try
        {
            return Send(Class("NSString"), Sel("stringWithUTF8String:"), utf8);
        }
        finally
        {
            Marshal.FreeCoTaskMem(utf8);
        }
    }

    /// <summary>Copies an NSString into a .NET string.</summary>
    public static string? ToStr(nint nsString) => nsString == 0 ? null : Marshal.PtrToStringUTF8(Send(nsString, Sel("UTF8String")));

    [StructLayout(LayoutKind.Sequential)]
    public readonly record struct NSPoint(double X, double Y);

    [StructLayout(LayoutKind.Sequential)]
    public readonly record struct NSSize(double Width, double Height);
}
