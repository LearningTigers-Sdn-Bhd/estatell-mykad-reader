namespace Estatell.MyKad;

/// <summary>
/// The holder's details, in the shape of <c>MyKadData</c> in Estatell's <c>use-mykad-reader.ts</c>.
/// The bridge writes the names in snake case.
/// </summary>
/// <param name="DateOfBirth"><c>YYYY-MM-DD</c>, or empty when the card holds no valid date.</param>
/// <param name="Gender"><c>male</c> or <c>female</c>.</param>
/// <param name="AddressLine2">Address lines 2 and 3, joined with <c>, </c>.</param>
/// <param name="State">The state name in title case.</param>
/// <param name="PostalCode">Five digits, or empty.</param>
/// <param name="PhotoBase64">A <c>data:image/jpeg;base64,…</c> string, or null when there is no JPEG portrait.</param>
public sealed record MyKadData(
    string Name,
    string IcNumber,
    string FormattedIc,
    string DateOfBirth,
    string Gender,
    string NationalityCode,
    string AddressLine1,
    string AddressLine2,
    string City,
    string State,
    string PostalCode,
    string CountryCode,
    string? PhotoBase64);
