using System.Text.Json;

namespace Estatell.MyKad.Tests.Card;

/// <summary>
/// A real card's JPN files, with the personal data replaced, must decode as the Python bridge decodes them.
/// <c>tools/parity/make_fixture.py</c> made the files and <c>python.json</c>.
/// </summary>
public class ParityTests
{
    private static readonly string _folder = Path.Combine(AppContext.BaseDirectory, "fixtures", "parity");

    [Fact]
    public void MatchesThePythonBridge()
    {
        var steps = CardFixture.Steps(File("jpn1-1.bin"), File("jpn1-4.bin"), File("jpn1-2.bin"));
        using var json = JsonDocument.Parse(System.IO.File.ReadAllText(Path.Combine(_folder, "python.json")));
        var python = json.RootElement;

        var data = MyKadCard.Read(CardFixture.Reader(steps), TestContext.Current.CancellationToken);

        Assert.Equal(Text(python, "name"), data.Name);
        Assert.Equal(Text(python, "raw_ic"), data.IcNumber);
        Assert.Equal(Text(python, "government_id"), data.FormattedIc);
        Assert.Equal(Blank(Text(python, "date_of_birth"), "0000-00-00"), data.DateOfBirth);
        Assert.Equal(Text(python, "gender"), data.Gender);
        Assert.Equal(Text(python, "address_line1"), data.AddressLine1);
        Assert.Equal(Decode.JoinLines(Text(python, "address_line2"), Text(python, "address_line3")), data.AddressLine2);
        Assert.Equal(Text(python, "city"), data.City);
        Assert.Equal(Text(python, "state_name"), data.State);
        Assert.Equal(Text(python, "state_code"), data.StateCode);
        Assert.Equal(Blank(Text(python, "postal_code"), "00000"), data.PostalCode);
        Assert.Equal(python.GetProperty("photo_base64").GetString(), data.PhotoBase64);
    }

    private static byte[] File(string name) => System.IO.File.ReadAllBytes(Path.Combine(_folder, name));

    /// <summary>Python's <c>strip()</c> keeps NUL padding. 2.0.0 trims it, see the Fields rules.</summary>
    private static string Text(JsonElement python, string name) =>
        python.GetProperty(name).GetString()!.Trim(' ', '\0');

    /// <summary>Python sends zeros for a blank date or postcode. 2.0.0 sends an empty string.</summary>
    private static string Blank(string value, string pythonBlank) => value == pythonBlank ? "" : value;
}
