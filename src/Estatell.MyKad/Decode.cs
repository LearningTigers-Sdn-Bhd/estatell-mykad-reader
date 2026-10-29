using System.Globalization;
using System.Text;

namespace Estatell.MyKad;

/// <summary>Turns the JPN file bytes into <see cref="MyKadData"/>. Offsets are from the Python bridge.</summary>
internal static class Decode
{
    private const string JpegPrefix = "data:image/jpeg;base64,";

    private static readonly char[] _padding = [' ', '\0', '\t', '\r', '\n'];

    public static MyKadData Data(ReadOnlySpan<byte> identity, ReadOnlySpan<byte> address, ReadOnlySpan<byte> portrait)
    {
        var ic = Text(identity, 0x111, 13);

        return new MyKadData(
            Name: Text(identity, 0x03, 150),
            IcNumber: ic,
            FormattedIc: FormatIc(ic),
            DateOfBirth: Date(identity.Slice(0x127, 4)),
            Gender: Gender(Text(identity, 0x11E, 1)),
            NationalityCode: "MY",
            AddressLine1: Text(address, 0x03, 30),
            AddressLine2: JoinLines(Text(address, 0x21, 30), Text(address, 0x3F, 30)),
            City: TitleCase(Text(address, 0x60, 25)),
            State: TitleCase(Text(address, 0x79, 30)),
            PostalCode: Postcode(address.Slice(0x5D, 3)),
            CountryCode: "MY",
            PhotoBase64: Photo(portrait));
    }

    /// <summary>ASCII text with the padding trimmed.</summary>
    public static string Text(ReadOnlySpan<byte> file, int offset, int length) =>
        Encoding.ASCII.GetString(file.Slice(offset, length)).Trim(_padding);

    /// <summary><c>######-##-####</c> for a 12-digit IC, else the IC without dashes.</summary>
    public static string FormatIc(string ic)
    {
        var clean = ic.Trim().Replace("-", "", StringComparison.Ordinal);
        return clean.Length == 12 ? $"{clean[..6]}-{clean[6..8]}-{clean[8..]}" : clean;
    }

    public static string Gender(string code) =>
        string.Equals(code, "L", StringComparison.OrdinalIgnoreCase) ? "male" : "female";

    /// <summary>BCD <c>YYYYMMDD</c> as <c>YYYY-MM-DD</c>. Empty when it is not a real date.</summary>
    public static string Date(ReadOnlySpan<byte> bcd)
    {
        if (!TryBcd(bcd, out var digits)
            || !DateOnly.TryParseExact(digits, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return "";
        }

        return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    /// <summary>Three BCD bytes hold the postcode and one more digit. Empty when blank or not BCD.</summary>
    public static string Postcode(ReadOnlySpan<byte> bcd)
    {
        if (!TryBcd(bcd, out var digits) || digits.All(d => d == '0'))
        {
            return "";
        }

        return digits[..5];
    }

    /// <summary>Address line 3 goes after line 2, so it is not lost.</summary>
    public static string JoinLines(string line2, string line3) =>
        string.Join(", ", new[] { line2, line3 }.Where(line => line.Length > 0));

    /// <summary>
    /// The same as Python's <c>str.title()</c>: a letter is upper case after a non-letter, else lower case.
    /// .NET's <c>ToTitleCase</c> leaves all-caps words alone, so it does not fit card text.
    /// </summary>
    public static string TitleCase(string value)
    {
        var result = new StringBuilder(value.Length);
        var afterLetter = false;
        foreach (var c in value)
        {
            var cased = char.IsUpper(c) || char.IsLower(c);
            result.Append(!cased ? c : afterLetter ? char.ToLowerInvariant(c) : char.ToUpperInvariant(c));
            afterLetter = cased;
        }

        return result.ToString();
    }

    /// <summary>The portrait as a data URL, or null when it is missing or not a JPEG.</summary>
    public static string? Photo(ReadOnlySpan<byte> portrait)
    {
        if (portrait.Length < 0x03 + 4000)
        {
            return null;
        }

        var jpeg = portrait.Slice(0x03, 4000);
        return jpeg[0] == 0xFF && jpeg[1] == 0xD8 ? JpegPrefix + Convert.ToBase64String(jpeg) : null;
    }

    private static bool TryBcd(ReadOnlySpan<byte> bcd, out string digits)
    {
        var chars = new char[bcd.Length * 2];
        for (var i = 0; i < bcd.Length; i++)
        {
            int high = bcd[i] >> 4, low = bcd[i] & 0x0F;
            if (high > 9 || low > 9)
            {
                digits = "";
                return false;
            }

            chars[i * 2] = (char)('0' + high);
            chars[(i * 2) + 1] = (char)('0' + low);
        }

        digits = new string(chars);
        return true;
    }
}
