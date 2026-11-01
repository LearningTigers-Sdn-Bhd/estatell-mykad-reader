namespace Estatell.MyKad.Bridge;

/// <summary>
/// Logs each request as method, path, status and origin. Never the query string, a header other than Origin, or a body,
/// so no card field can reach the log through a request. The check-in page polls <c>/status</c> every 2.5 seconds, so a
/// successful <c>/status</c> is logged at Debug (<c>--verbose</c>) and every other request at Information.
/// </summary>
internal static partial class RequestLog
{
    private const int MaxLength = 200;

    public static async Task InvokeAsync(HttpContext context, RequestDelegate next, ILogger logger)
    {
        await next(context);

        var request = context.Request;
        var method = Clean(request.Method);
        var path = Clean(request.Path.Value);
        var origin = request.Headers.Origin.Count == 0 ? "-" : Clean(request.Headers.Origin.ToString());
        var status = context.Response.StatusCode;

        if (status < 400 && request.Path.Value is "/" or "/status" or "/health")
        {
            LogQuiet(logger, method, path, status, origin);
        }
        else
        {
            LogRequest(logger, method, path, status, origin);
        }
    }

    /// <summary>A header value or path is the caller's text. Cut it and drop control characters so it cannot forge a log line.</summary>
    private static string Clean(string? text)
    {
        text ??= "";
        var cut = text.Length > MaxLength ? text[..MaxLength] : text;
        return string.Create(cut.Length, cut, static (span, source) =>
        {
            for (var i = 0; i < source.Length; i++)
            {
                span[i] = char.IsControl(source[i]) ? '?' : source[i];
            }
        });
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{Method} {Path} -> {Status} origin={Origin}")]
    private static partial void LogRequest(ILogger logger, string method, string path, int status, string origin);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{Method} {Path} -> {Status} origin={Origin}")]
    private static partial void LogQuiet(ILogger logger, string method, string path, int status, string origin);
}
