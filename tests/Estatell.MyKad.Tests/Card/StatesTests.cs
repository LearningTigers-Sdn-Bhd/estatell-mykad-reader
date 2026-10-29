namespace Estatell.MyKad.Tests.Card;

public class StatesTests
{
    [Theory]
    [InlineData("JOHOR", "01")]
    [InlineData("KEDAH", "02")]
    [InlineData("KELANTAN", "03")]
    [InlineData("MELAKA", "04")]
    [InlineData("MALACCA", "04")]
    [InlineData("NEGERI SEMBILAN", "05")]
    [InlineData("N. SEMBILAN", "05")]
    [InlineData("PAHANG", "06")]
    [InlineData("PULAU PINANG", "07")]
    [InlineData("PENANG", "07")]
    [InlineData("PERAK", "08")]
    [InlineData("PERLIS", "09")]
    [InlineData("SELANGOR", "10")]
    [InlineData("TERENGGANU", "11")]
    [InlineData("SABAH", "12")]
    [InlineData("SARAWAK", "13")]
    [InlineData("KUALA LUMPUR", "14")]
    [InlineData("W.P. KUALA LUMPUR", "14")]
    [InlineData("WILAYAH PERSEKUTUAN KUALA LUMPUR", "14")]
    [InlineData("LABUAN", "15")]
    [InlineData("W.P. LABUAN", "15")]
    [InlineData("WILAYAH PERSEKUTUAN LABUAN", "15")]
    [InlineData("PUTRAJAYA", "16")]
    [InlineData("W.P. PUTRAJAYA", "16")]
    [InlineData("WILAYAH PERSEKUTUAN PUTRAJAYA", "16")]
    public void MapsEachStateName(string state, string code) => Assert.Equal(code, States.Code(state, ""));

    [Theory]
    [InlineData("  sabah ", "12")]
    [InlineData("Pulau Pinang", "07")]
    [InlineData("88300 KOTA KINABALU, SABAH", "12")]
    public void MatchesAnyCaseAndInsideOtherText(string state, string code) => Assert.Equal(code, States.Code(state, ""));

    [Fact]
    public void TheStateTextWinsOverThePostcode() => Assert.Equal("01", States.Code("JOHOR", "88300"));

    [Theory]
    [InlineData("88000", "12")]
    [InlineData("91999", "12")]
    [InlineData("93000", "13")]
    [InlineData("98999", "13")]
    [InlineData("50000", "14")]
    [InlineData("60999", "14")]
    [InlineData("87000", "15")]
    [InlineData("62000", "16")]
    [InlineData("40000", "10")]
    [InlineData("48999", "10")]
    [InlineData("63000", "10")]
    [InlineData("68999", "10")]
    [InlineData("80000", "01")]
    [InlineData("86999", "01")]
    [InlineData("30000", "08")]
    [InlineData("39999", "08")]
    [InlineData("10000", "07")]
    [InlineData("14999", "07")]
    public void FallsBackToThePostcode(string postcode, string code) => Assert.Equal(code, States.Code("UNKNOWN", postcode));

    [Theory]
    [InlineData("UNKNOWN", "05000")] // Kedah has no range in the Python bridge.
    [InlineData("UNKNOWN", "49000")]
    [InlineData("UNKNOWN", "61000")]
    [InlineData("UNKNOWN", "92000")]
    [InlineData("UNKNOWN", "99000")]
    [InlineData("UNKNOWN", "")]
    [InlineData("UNKNOWN", "8830")]
    [InlineData("UNKNOWN", "88A00")]
    [InlineData("", "")]
    public void NoMatchIsEmpty(string state, string postcode) => Assert.Equal("", States.Code(state, postcode));
}
