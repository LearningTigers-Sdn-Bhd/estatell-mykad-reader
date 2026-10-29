using System.Diagnostics;
using System.Text;
using PCSC;

// Reads JPN1-1 (and with --full JPN1-4 and JPN1-2) the way mykad_bridge.py 1.1.0 does.
// Personal data is masked unless --show is passed.
var full = args.Contains("--full");
var show = args.Contains("--show");

Console.WriteLine($"Runtime: {System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier}, AOT: {string.IsNullOrEmpty(typeof(Program).Assembly.Location)}");

var total = Stopwatch.StartNew();
using var context = ContextFactory.Instance.Establish(SCardScope.System);

var readers = context.GetReaders();
Console.WriteLine($"Readers: {string.Join(", ", readers)}");
if (readers.Length == 0)
{
    return 1;
}

var share = args.Contains("--shared") ? SCardShareMode.Shared : SCardShareMode.Exclusive;
var protocol = args.Contains("--t0") ? SCardProtocol.T0 : args.Contains("--t1") ? SCardProtocol.T1 : SCardProtocol.Any;
Console.WriteLine($"Connecting: {share}, {protocol}");
using var reader = context.ConnectReader(readers[0], share, protocol);
Console.WriteLine($"Connected: protocol {reader.Protocol}, {total.ElapsedMilliseconds} ms");

Send("Select JPN", [0x00, 0xA4, 0x04, 0x00, 0x0A, 0xA0, 0x00, 0x00, 0x00, 0x74, 0x4A, 0x50, 0x4E, 0x00, 0x10], 0x90, 0x61);

var jpn11 = ReadFile("JPN1-1", 1, 1, 459);
var name = Ascii(jpn11, 3, 150);
var ic = Ascii(jpn11, 0x111, 13);
var gender = Ascii(jpn11, 0x11E, 1);
Console.WriteLine($"Name: {Mask(name)}");
Console.WriteLine($"IC: {Mask(ic)}");
Console.WriteLine($"Gender code: {gender}");

if (full)
{
    var jpn14 = ReadFile("JPN1-4", 1, 4, 171);
    Console.WriteLine($"City: {Mask(Ascii(jpn14, 0x60, 25))}, state: {Ascii(jpn14, 0x79, 30)}");

    var jpn12 = ReadFile("JPN1-2", 1, 2, 4011);
    var isJpeg = jpn12[3] == 0xFF && jpn12[4] == 0xD8;
    Console.WriteLine($"Portrait: {(isJpeg ? "JPEG" : "not JPEG")}, {jpn12.Length - 11} bytes");
    Array.Clear(jpn14);
    Array.Clear(jpn12);
}

Array.Clear(jpn11);
Console.WriteLine($"Total: {total.ElapsedMilliseconds} ms");
return 0;

byte[] ReadFile(string label, ushort file1, ushort file2, ushort length)
{
    var sw = Stopwatch.StartNew();
    Send($"{label} set length", [0xC8, 0x32, 0x00, 0x00, 0x05, 0x08, 0x00, 0x00, .. Le16(length)], 0x90, 0x91);
    Send($"{label} select", [0xCC, 0x00, 0x00, 0x00, 0x08, .. Le16(file2), .. Le16(file1), .. Le16(0), .. Le16(length)], 0x90, 0x91, 0x94);

    var data = new List<byte>(length);
    var chunks = 0;
    while (data.Count < length)
    {
        var chunk = (byte)Math.Min(length - data.Count, 255);
        data.AddRange(Send(null, [0xCC, 0x06, 0x00, 0x00, chunk], 0x90, 0x91, 0x94));
        chunks++;
    }

    Console.WriteLine($"{label}: {data.Count} bytes in {chunks} chunks, {sw.ElapsedMilliseconds} ms");
    return [.. data];
}

byte[] Send(string? label, byte[] command, params byte[] acceptSw1)
{
    var receive = new byte[258];
    var received = reader.Transmit(command, receive);
    if (received < 2)
    {
        throw new InvalidOperationException($"{label ?? "Read"}: {received} bytes back");
    }

    var sw1 = receive[received - 2];
    var sw2 = receive[received - 1];
    if (label is not null)
    {
        Console.WriteLine($"  {label}: SW {sw1:X2} {sw2:X2}");
    }

    if (!acceptSw1.Contains(sw1))
    {
        throw new InvalidOperationException($"{label ?? "Read"} failed: SW {sw1:X2} {sw2:X2}");
    }

    return receive[..(received - 2)];
}

static byte[] Le16(ushort value) => [(byte)value, (byte)(value >> 8)];

static string Ascii(byte[] data, int offset, int length) =>
    Encoding.ASCII.GetString(data, offset, length).Trim('\0', ' ');

string Mask(string value) =>
    show ? value : $"{(value.Length > 0 ? value[0] : '?')}*** ({value.Length} chars)";
