using System.Text;
using Estatell.MyKad.Reader;

namespace Estatell.MyKad.Tests.Card;

/// <summary>
/// A made-up card: the JPN files built at the real offsets with fake details, and the
/// commands and responses of a full read. No real person's data.
/// </summary>
internal sealed class CardFixture
{
    public const string Marker = "NOT-RETURNED";

    public string Name { get; init; } = "SITI AMINAH BINTI ABU BAKAR";
    public string Ic { get; init; } = "900101125566";
    public string GenderCode { get; init; } = "P";
    public byte[] DateOfBirth { get; init; } = [0x19, 0x90, 0x01, 0x01];
    public string Address1 { get; init; } = "LOT 12 JALAN CONTOH";
    public string Address2 { get; init; } = "TAMAN UJIAN";
    public string Address3 { get; init; } = "BATU 5";
    public byte[] Postcode { get; init; } = [0x88, 0x30, 0x00];
    public string City { get; init; } = "KOTA KINABALU";
    public string State { get; init; } = "SABAH";
    public byte[] Jpeg { get; init; } = FakeJpeg();

    /// <summary>One step of the read: its name, the command, and the data the card sends back.</summary>
    public sealed record Step(string Name, byte[] Command, byte[] Data);

    public byte[] Identity()
    {
        var file = new byte[459];
        Put(file, 0x03, 150, Name);
        Put(file, 0x111, 13, Ic);
        Put(file, 0x11E, 1, GenderCode);
        Put(file, 0x11F, 8, Marker[..8]);
        DateOfBirth.CopyTo(file, 0x127);
        Put(file, 0x12B, 25, Marker);
        Put(file, 0x148, 18, Marker);
        Put(file, 0x15A, 25, Marker);
        Put(file, 0x173, 11, Marker[..11]);
        return file;
    }

    public byte[] Address()
    {
        var file = new byte[171];
        Put(file, 0x03, 30, Address1);
        Put(file, 0x21, 30, Address2);
        Put(file, 0x3F, 30, Address3);
        Postcode.CopyTo(file, 0x5D);
        Put(file, 0x60, 25, City);
        Put(file, 0x79, 30, State);
        return file;
    }

    public byte[] Portrait()
    {
        var file = new byte[4011];
        Jpeg.CopyTo(file, 0x03);
        return file;
    }

    /// <summary>Every command of a full read, in order.</summary>
    public IReadOnlyList<Step> Steps() => Steps(Identity(), Address(), Portrait());

    /// <summary>Every command of a full read of a card that holds these files, in order.</summary>
    public static IReadOnlyList<Step> Steps(byte[] identity, byte[] address, byte[] portrait) =>
        [
            new("Select JPN", Apdu.SelectJpn, []),
            .. FileSteps(JpnFile.Identity, identity),
            .. FileSteps(JpnFile.Address, address),
            .. FileSteps(JpnFile.Portrait, portrait),
        ];

    /// <summary>A reader that answers a full read. The step at <paramref name="failAt"/> gets <c>6A 82</c> and ends the queue.</summary>
    public FakeReader Reader(int? failAt = null) => Reader(Steps(), failAt);

    /// <inheritdoc cref="Reader(int?)"/>
    public static FakeReader Reader(IReadOnlyList<Step> steps, int? failAt = null)
    {
        var reader = new FakeReader();
        for (var i = 0; i < steps.Count; i++)
        {
            if (i == failAt)
            {
                reader.Expect(steps[i].Command, [], 0x6A, 0x82);
                break;
            }

            reader.Expect(steps[i].Command, steps[i].Data, steps[i].Name == "Select JPN" ? (byte)0x61 : (byte)0x90, 0x00);
        }

        return reader;
    }

    public static byte[] FakeJpeg()
    {
        var jpeg = new byte[4000];
        jpeg[0] = 0xFF;
        jpeg[1] = 0xD8;
        for (var i = 2; i < jpeg.Length; i++)
        {
            jpeg[i] = (byte)i;
        }

        return jpeg;
    }

    private static IEnumerable<Step> FileSteps(JpnFile file, byte[] data)
    {
        yield return new($"{file.Name} set length", Apdu.SetLength(file.Length), []);
        yield return new($"{file.Name} select", Apdu.SelectFile(file), []);
        for (var read = 0; read < data.Length; read += Apdu.MaxChunk)
        {
            var chunk = Math.Min(data.Length - read, Apdu.MaxChunk);
            yield return new($"{file.Name} read", Apdu.ReadChunk((byte)chunk), data[read..(read + chunk)]);
        }
    }

    /// <summary>Space-padded ASCII, as on the card.</summary>
    private static void Put(byte[] file, int offset, int length, string value)
    {
        file.AsSpan(offset, length).Fill((byte)' ');
        Encoding.ASCII.GetBytes(value).CopyTo(file, offset);
    }
}
