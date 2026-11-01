namespace Estatell.MyKad.Bridge;

/// <summary>
/// When a request with an allowed <c>Origin</c> last came in, and from which host. The tray shows it as the Estatell line.
/// See docs/02-bridge/04-tray.md#menu.
/// </summary>
internal sealed class EstatellPresence(TimeProvider clock)
{
    /// <summary>The check-in page polls every 2.5 seconds, so 10 seconds covers a few missed polls.</summary>
    public static readonly TimeSpan ConnectedFor = TimeSpan.FromSeconds(10);

    private volatile Seen? _last;

    /// <summary>Call only for a request whose <c>Origin</c> the policy allowed.</summary>
    public void Record(string origin)
    {
        if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
        {
            _last = new Seen(uri.Host, clock.GetUtcNow());
        }
    }

    /// <summary>The host of the last allowed origin while it is recent, or null for "not seen".</summary>
    public string? ConnectedHost =>
        _last is { } seen && clock.GetUtcNow() - seen.At <= ConnectedFor ? seen.Host : null;

    private sealed record Seen(string Host, DateTimeOffset At);
}
