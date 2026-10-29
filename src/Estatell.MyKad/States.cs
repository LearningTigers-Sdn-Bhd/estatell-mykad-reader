namespace Estatell.MyKad;

/// <summary>
/// Maps the card's state text to the LHDN state code, as in <c>EInvoice::MalaysiaStates</c> in Estatell.
/// Ported from <c>STATE_CODES</c> and <c>resolve_state_code()</c> in the Python bridge 1.1.0.
/// </summary>
internal static class States
{
    // Checked in this order. A name matches when the state text contains it.
    private static readonly (string Name, string Code)[] _codes =
    [
        ("JOHOR", "01"),
        ("KEDAH", "02"),
        ("KELANTAN", "03"),
        ("MELAKA", "04"),
        ("MALACCA", "04"),
        ("NEGERI SEMBILAN", "05"),
        ("N. SEMBILAN", "05"),
        ("PAHANG", "06"),
        ("PULAU PINANG", "07"),
        ("PENANG", "07"),
        ("PERAK", "08"),
        ("PERLIS", "09"),
        ("SELANGOR", "10"),
        ("TERENGGANU", "11"),
        ("SABAH", "12"),
        ("SARAWAK", "13"),
        ("KUALA LUMPUR", "14"),
        ("W.P. KUALA LUMPUR", "14"),
        ("WILAYAH PERSEKUTUAN KUALA LUMPUR", "14"),
        ("LABUAN", "15"),
        ("W.P. LABUAN", "15"),
        ("WILAYAH PERSEKUTUAN LABUAN", "15"),
        ("PUTRAJAYA", "16"),
        ("W.P. PUTRAJAYA", "16"),
        ("WILAYAH PERSEKUTUAN PUTRAJAYA", "16"),
    ];

    // Used when the state text does not match. The first two postcode digits, inclusive.
    private static readonly (int From, int To, string Code)[] _postcodes =
    [
        (88, 91, "12"), // Sabah
        (93, 98, "13"), // Sarawak
        (50, 60, "14"), // Kuala Lumpur
        (87, 87, "15"), // Labuan
        (62, 62, "16"), // Putrajaya
        (40, 48, "10"), // Selangor
        (63, 68, "10"), // Selangor
        (80, 86, "01"), // Johor
        (30, 39, "08"), // Perak
        (10, 14, "07"), // Penang
    ];

    /// <summary>The LHDN code for the state text, else for the postcode, else empty.</summary>
    public static string Code(string state, string postcode)
    {
        var clean = state.Trim().ToUpperInvariant();
        foreach (var (name, code) in _codes)
        {
            if (clean.Contains(name, StringComparison.Ordinal))
            {
                return code;
            }
        }

        if (postcode.Length != 5 || !postcode.All(char.IsAsciiDigit))
        {
            return "";
        }

        var prefix = ((postcode[0] - '0') * 10) + (postcode[1] - '0');
        foreach (var (from, to, code) in _postcodes)
        {
            if (prefix >= from && prefix <= to)
            {
                return code;
            }
        }

        return "";
    }
}
