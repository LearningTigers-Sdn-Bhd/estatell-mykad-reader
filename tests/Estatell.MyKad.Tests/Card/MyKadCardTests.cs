using Estatell.MyKad.Reader;

namespace Estatell.MyKad.Tests.Card;

public class MyKadCardTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public void ReadsEveryField()
    {
        var reader = new CardFixture().Reader();

        var data = MyKadCard.Read(reader, Token);

        Assert.Equal(
            new MyKadData(
                Name: "SITI AMINAH BINTI ABU BAKAR",
                IcNumber: "900101125566",
                FormattedIc: "900101-12-5566",
                DateOfBirth: "1990-01-01",
                Gender: "female",
                NationalityCode: "MY",
                AddressLine1: "LOT 12 JALAN CONTOH",
                AddressLine2: "TAMAN UJIAN, BATU 5",
                City: "Kota Kinabalu",
                State: "Sabah",
                StateCode: "12",
                PostalCode: "88300",
                CountryCode: "MY",
                PhotoBase64: "data:image/jpeg;base64," + Convert.ToBase64String(CardFixture.FakeJpeg())),
            data);
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void SendsTheCommandsOfThePythonBridge()
    {
        var reader = new CardFixture().Reader();

        MyKadCard.Read(reader, Token);

        // Select JPN, then per file: set length, select, and 255-byte chunks.
        Assert.Equal(1 + (2 + 2) + (2 + 1) + (2 + 16), reader.Sent.Count);
        Assert.Equal(Convert.FromHexString("00A404000AA0000000744A504E0010"), reader.Sent[0]);
        Assert.Equal(Convert.FromHexString("C832000005080000CB01"), reader.Sent[1]);
        Assert.Equal(Convert.FromHexString("CC00000008010001000000CB01"), reader.Sent[2]);
        Assert.Equal(Convert.FromHexString("CC060000FF"), reader.Sent[3]);
        Assert.Equal(Convert.FromHexString("CC060000CC"), reader.Sent[4]);
        Assert.Equal(Convert.FromHexString("C832000005080000AB00"), reader.Sent[5]);
        Assert.Equal(Convert.FromHexString("CC00000008040001000000AB00"), reader.Sent[6]);
        Assert.Equal(Convert.FromHexString("CC060000AB"), reader.Sent[7]);
        Assert.Equal(Convert.FromHexString("C832000005080000AB0F"), reader.Sent[8]);
        Assert.Equal(Convert.FromHexString("CC00000008020001000000AB0F"), reader.Sent[9]);
        Assert.Equal(Convert.FromHexString("CC060000BA"), reader.Sent[^1]);
    }

    [Fact]
    public void DoesNotReturnTheFieldsEstatellDoesNotUse()
    {
        var data = MyKadCard.Read(new CardFixture().Reader(), Token);

        Assert.DoesNotContain(CardFixture.Marker[..8], data.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void AMaleCardSaysMale()
    {
        var data = MyKadCard.Read(new CardFixture { GenderCode = "L" }.Reader(), Token);

        Assert.Equal("male", data.Gender);
    }

    [Fact]
    public void AnEmptyLineThreeLeavesLineTwo()
    {
        var data = MyKadCard.Read(new CardFixture { Address3 = "" }.Reader(), Token);

        Assert.Equal("TAMAN UJIAN", data.AddressLine2);
    }

    [Fact]
    public void AnUnknownStateUsesThePostcode()
    {
        var data = MyKadCard.Read(new CardFixture { State = "NEGERI X", Postcode = [0x50, 0x48, 0x00] }.Reader(), Token);

        Assert.Equal(("Negeri X", "14", "50480"), (data.State, data.StateCode, data.PostalCode));
    }

    [Fact]
    public void ANonJpegPortraitGivesNoPhoto()
    {
        var data = MyKadCard.Read(new CardFixture { Jpeg = new byte[4000] }.Reader(), Token);

        Assert.Null(data.PhotoBase64);
        Assert.Equal("SITI AMINAH BINTI ABU BAKAR", data.Name);
    }

    [Theory]
    [InlineData("Select JPN")]
    [InlineData("JPN1-1 set length")]
    [InlineData("JPN1-1 select")]
    [InlineData("JPN1-1 read")]
    [InlineData("JPN1-4 set length")]
    [InlineData("JPN1-4 select")]
    [InlineData("JPN1-4 read")]
    public void ARefusedStepThrowsWithItsNameAndStatusWords(string step)
    {
        var fixture = new CardFixture();
        var reader = fixture.Reader(failAt: IndexOf(fixture, step));

        var e = Assert.Throws<CardReadException>(() => MyKadCard.Read(reader, Token));

        Assert.Equal(step, e.Step);
        Assert.Equal((0x6A, 0x82), (e.Sw1, e.Sw2));
        Assert.Equal($"{step} failed (SW 6A 82).", e.Message);
    }

    [Theory]
    [InlineData("JPN1-2 set length")]
    [InlineData("JPN1-2 select")]
    [InlineData("JPN1-2 read")]
    public void ARefusedPortraitGivesNoPhoto(string step)
    {
        var fixture = new CardFixture();

        var data = MyKadCard.Read(fixture.Reader(failAt: IndexOf(fixture, step)), Token);

        Assert.Null(data.PhotoBase64);
        Assert.Equal("900101125566", data.IcNumber);
        Assert.Equal("88300", data.PostalCode);
    }

    [Fact]
    public void AShortChunkThrows()
    {
        var fixture = new CardFixture();
        var steps = fixture.Steps();
        var reader = new FakeReader()
            .Expect(steps[0].Command, [], 0x61, 0x05)
            .Expect(steps[1].Command, [], 0x90, 0x00)
            .Expect(steps[2].Command, [], 0x90, 0x00)
            .Expect(steps[3].Command, new byte[10], 0x90, 0x00);

        var e = Assert.Throws<CardReadException>(() => MyKadCard.Read(reader, Token));

        Assert.Equal("JPN1-1 read", e.Step);
        Assert.Contains("10 of 255 bytes", e.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ClearsTheCardResponses()
    {
        var steps = new CardFixture().Steps();

        // FakeReader hands back the queued arrays, so a cleared response shows in the steps.
        MyKadCard.Read(CardFixture.Reader(steps), Token);

        Assert.All(steps.SelectMany(s => s.Data), b => Assert.Equal(0, b));
    }

    [Fact]
    public void ReaderProblemsPassThrough()
    {
        var reader = new FakeReader { Status = new ReaderStatus("Fake Reader 0", false) };

        var e = Assert.Throws<ReaderException>(() => MyKadCard.Read(reader, Token));

        Assert.Equal(ReaderErrorCode.NoCard, e.Code);
    }

    private static int IndexOf(CardFixture fixture, string step) =>
        fixture.Steps().Select((s, i) => (s.Name, i)).First(s => s.Name == step).i;
}
