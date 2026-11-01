using Microsoft.Extensions.Primitives;

namespace Estatell.MyKad.Bridge;

/// <summary>
/// The Host and Origin checks and the CORS preflight. Runs before every route, so a refused request never reaches the card.
/// See docs/02-bridge/02-security.md.
/// </summary>
internal static partial class RequestChecks
{
    private static readonly string[] _hosts = [$"127.0.0.1:{BridgeInfo.Port}", $"localhost:{BridgeInfo.Port}"];

    public static async Task InvokeAsync(HttpContext context, RequestDelegate next, OriginPolicy policy, ILogger logger)
    {
        var request = context.Request;
        var response = context.Response;

        // DNS rebinding: a foreign name that resolves to 127.0.0.1 still sends its own Host.
        var host = request.Host.Value ?? "";
        if (!_hosts.Contains(host, StringComparer.OrdinalIgnoreCase))
        {
            LogRefusedHost(logger, host);
            await Forbid(response, "This host is not allowed.");
            return;
        }

        // No Origin passes, as in 1.x (curl on this computer). /read and /pair still need the token or the code.
        var origin = request.Headers.Origin;
        if (!StringValues.IsNullOrEmpty(origin))
        {
            var text = origin.ToString();
            if (origin.Count != 1 || !policy.IsAllowed(text))
            {
                LogRefusedOrigin(logger, text);
                await Forbid(response, "This website is not allowed.");
                return;
            }

            response.Headers.AccessControlAllowOrigin = origin;
            response.Headers.Vary = "Origin";
        }

        if (HttpMethods.IsOptions(request.Method))
        {
            if (!StringValues.IsNullOrEmpty(origin))
            {
                response.Headers.AccessControlAllowMethods = "GET, POST, OPTIONS";
                response.Headers.AccessControlAllowHeaders = "Content-Type";
                response.Headers.AccessControlMaxAge = "600";

                // Chrome's Private Network Access: an HTTPS page asks before it calls 127.0.0.1.
                if (request.Headers.ContainsKey("Access-Control-Request-Private-Network"))
                {
                    response.Headers["Access-Control-Allow-Private-Network"] = "true";
                }
            }

            response.StatusCode = StatusCodes.Status204NoContent;
            return;
        }

        await next(context);
    }

    private static Task Forbid(HttpResponse response, string error)
    {
        response.StatusCode = StatusCodes.Status403Forbidden;
        return response.WriteAsJsonAsync(new BridgeError(false, "forbidden", error), BridgeJson.Default.BridgeError);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Refused host '{Host}'.")]
    private static partial void LogRefusedHost(ILogger logger, string host);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Refused origin '{Origin}'.")]
    private static partial void LogRefusedOrigin(ILogger logger, string origin);
}
