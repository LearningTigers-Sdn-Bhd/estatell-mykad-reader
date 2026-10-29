namespace Estatell.MyKad.Bridge;

/// <summary>
/// Which web pages may call the bridge: the Estatell sites, local development hosts
/// (<c>localhost</c>, <c>127.0.0.1</c>, <c>*.test</c>, any port) and the origins in <c>ESTATELL_BRIDGE_ORIGINS</c>.
/// </summary>
internal sealed class OriginPolicy
{
    public const string EnvironmentVariable = "ESTATELL_BRIDGE_ORIGINS";

    private static readonly string[] _estatell = ["https://app.estatell.com", "https://demo.estatell.com"];

    private readonly HashSet<string> _exact = new(StringComparer.OrdinalIgnoreCase);

    /// <param name="extraOrigins">Comma-separated origins, as in <see cref="EnvironmentVariable"/>.</param>
    public OriginPolicy(string? extraOrigins = null)
    {
        foreach (var origin in _estatell)
        {
            _exact.Add(origin);
        }

        var skipped = new List<string>();
        foreach (var entry in (extraOrigins ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Normalize(entry.TrimEnd('/')) is { } origin)
            {
                _exact.Add(origin);
            }
            else
            {
                skipped.Add(entry);
            }
        }

        Skipped = skipped;
    }

    /// <summary>Entries of <c>extraOrigins</c> that are not a plain <c>http(s)://host[:port]</c>.</summary>
    public IReadOnlyList<string> Skipped { get; }

    public bool IsAllowed(string origin)
    {
        if (Normalize(origin) is not { } normalized)
        {
            return false;
        }

        if (_exact.Contains(normalized))
        {
            return true;
        }

        // Uri lowercases the host, and a look-alike such as foo.test.evil.com does not end in .test.
        var host = new Uri(normalized).Host;
        return host is "localhost" or "127.0.0.1" || (host.EndsWith(".test", StringComparison.Ordinal) && host.Length > ".test".Length);
    }

    /// <summary><c>scheme://host[:port]</c> in lower case, or null when <paramref name="origin"/> is anything more or less.</summary>
    private static string? Normalize(string origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || uri.UserInfo.Length > 0)
        {
            return null;
        }

        var normalized = uri.GetLeftPart(UriPartial.Authority);

        // Same text apart from case and a default port, so no path, query or fragment came along.
        return string.Equals(normalized, origin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, StripDefaultPort(origin, uri), StringComparison.OrdinalIgnoreCase)
            ? normalized.ToLowerInvariant()
            : null;
    }

    private static string StripDefaultPort(string origin, Uri uri) =>
        uri.IsDefaultPort && origin.EndsWith($":{uri.Port}", StringComparison.Ordinal) ? origin[..^$":{uri.Port}".Length] : origin;
}
