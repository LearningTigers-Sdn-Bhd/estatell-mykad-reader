namespace Estatell.MyKad.Tests.Card;

public class DecodeTests
{
    [Theory]
    [InlineData("AHMAD   ", "AHMAD")]
    [InlineData("AHMAD\0\0\0", "AHMAD")]
    [InlineData("  \0 ", "")]
    public void TextTrimsSpacesAndNulls(string raw, string expected)
    {
        var file = System.Text.Encoding.ASCII.GetBytes($"XY{raw}Z");

        Assert.Equal(expected, Decode.Text(file, 2, raw.Length));
    }

    [Theory]
    [InlineData("900101125566", "900101-12-5566")]
    [InlineData("900101-12-5566", "900101-12-5566")]
    [InlineData("A1234567", "A1234567")]
    [InlineData("", "")]
    public void FormatsTheIc(string ic, string expected) => Assert.Equal(expected, Decode.FormatIc(ic));

    [Theory]
    [InlineData("L", "male")]
    [InlineData("l", "male")]
    [InlineData("P", "female")]
    [InlineData("", "female")]
    public void MapsTheGender(string code, string expected) => Assert.Equal(expected, Decode.Gender(code));

    [Theory]
    [InlineData(new byte[] { 0x19, 0x90, 0x01, 0x31 }, "1990-01-31")]
    [InlineData(new byte[] { 0x20, 0x04, 0x02, 0x29 }, "2004-02-29")]
    [InlineData(new byte[] { 0x19, 0x90, 0x13, 0x01 }, "")]
    [InlineData(new byte[] { 0x19, 0x90, 0x02, 0x30 }, "")]
    [InlineData(new byte[] { 0x00, 0x00, 0x00, 0x00 }, "")]
    [InlineData(new byte[] { 0x19, 0x9A, 0x01, 0x01 }, "")]
    public void ReadsTheBcdDate(byte[] bcd, string expected) => Assert.Equal(expected, Decode.Date(bcd));

    [Theory]
    [InlineData(new byte[] { 0x88, 0x30, 0x00 }, "88300")]
    [InlineData(new byte[] { 0x01, 0x00, 0x00 }, "01000")]
    [InlineData(new byte[] { 0x50, 0x48, 0x01 }, "50480")]
    [InlineData(new byte[] { 0x00, 0x00, 0x00 }, "")]
    [InlineData(new byte[] { 0x88, 0xF0, 0x00 }, "")]
    public void ReadsTheBcdPostcode(byte[] bcd, string expected) => Assert.Equal(expected, Decode.Postcode(bcd));

    [Theory]
    [InlineData("TAMAN UJIAN", "BATU 5", "TAMAN UJIAN, BATU 5")]
    [InlineData("TAMAN UJIAN", "", "TAMAN UJIAN")]
    [InlineData("", "BATU 5", "BATU 5")]
    [InlineData("", "", "")]
    public void JoinsLineThreeToLineTwo(string line2, string line3, string expected) =>
        Assert.Equal(expected, Decode.JoinLines(line2, line3));

    [Theory]
    [InlineData("KOTA KINABALU", "Kota Kinabalu")]
    [InlineData("W.P. KUALA LUMPUR", "W.P. Kuala Lumpur")]
    [InlineData("KG. O'BRIEN", "Kg. O'Brien")]
    [InlineData("1ST AVENUE", "1St Avenue")]
    [InlineData("pulau pinang", "Pulau Pinang")]
    [InlineData("", "")]
    public void TitleCaseMatchesPython(string value, string expected) => Assert.Equal(expected, Decode.TitleCase(value));

    [Fact]
    public void ReturnsAJpegPortraitAsADataUrl()
    {
        var portrait = new CardFixture().Portrait();

        var photo = Decode.Photo(portrait);

        Assert.Equal("data:image/jpeg;base64," + Convert.ToBase64String(CardFixture.FakeJpeg()), photo);
    }

    [Fact]
    public void ANonJpegPortraitIsNull()
    {
        var portrait = new CardFixture { Jpeg = new byte[4000] }.Portrait();

        Assert.Null(Decode.Photo(portrait));
    }

    [Fact]
    public void AMissingPortraitIsNull() => Assert.Null(Decode.Photo([]));
}
