using System.Diagnostics;
using Estatell.MyKad.Reader;
using Microsoft.Extensions.Primitives;

namespace Estatell.MyKad.Bridge;

/// <param name="Timeout">How long a read may wait for the reader and run. 10 seconds, as in the Python bridge.</param>
internal sealed record ReadOptions(TimeSpan Timeout)
{
    public static readonly ReadOptions Default = new(TimeSpan.FromSeconds(10));
}

/// <summary>
/// <c>GET</c> and <c>POST /read</c>. Needs an allowed <c>Origin</c>: <see cref="RequestChecks"/> refuses a foreign one,
/// and this route refuses a missing one. See docs/02-bridge/01-http-api.md#get-read.
/// </summary>
internal static partial class ReadRoute
{
    public static async Task<IResult> Handle(HttpContext context, IReader reader, ReadOptions options, ILoggerFactory loggers)
    {
        if (StringValues.IsNullOrEmpty(context.Request.Headers.Origin))
        {
            return Fail(StatusCodes.Status403Forbidden, "forbidden", "This website is not allowed.");
        }

        var logger = loggers.CreateLogger("Read");
        var started = Stopwatch.GetTimestamp();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
        timeout.CancelAfter(options.Timeout);

        try
        {
            // The PC/SC calls block, so keep them off the request thread.
            var data = await Task.Run(() => MyKadCard.Read(reader, timeout.Token), CancellationToken.None);
            var milliseconds = (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            LogRead(logger, milliseconds);
            return Results.Json(new ReadResponse(true, data), BridgeJson.Default.ReadResponse);
        }
        catch (ReaderException e)
        {
            LogFailed(logger, e.Code.ToString(), e);
            return e.Code switch
            {
                ReaderErrorCode.NoReader => Fail(StatusCodes.Status400BadRequest, "no_reader", "No card reader is plugged in."),
                ReaderErrorCode.NoCard => Fail(StatusCodes.Status400BadRequest, "no_card", "No card in the reader."),
                ReaderErrorCode.Busy => Fail(StatusCodes.Status409Conflict, "busy", $"Another read did not finish in {options.Timeout.TotalSeconds:0} seconds."),
                _ => Fail(StatusCodes.Status500InternalServerError, "read_failed", "The card reader could not be used."),
            };
        }
        catch (CardReadException e)
        {
            LogFailed(logger, "CardStep", e);
            return Fail(StatusCodes.Status500InternalServerError, "read_failed", e.Message);
        }
        catch (OperationCanceledException e)
        {
            LogFailed(logger, "Timeout", e);
            return Fail(StatusCodes.Status500InternalServerError, "read_failed", $"The read did not finish in {options.Timeout.TotalSeconds:0} seconds.");
        }
        catch (Exception e)
        {
            // Never send the text of an unexpected exception to a web page.
            LogFailed(logger, "Unexpected", e);
            return Fail(StatusCodes.Status500InternalServerError, "read_failed", "The card could not be read.");
        }
    }

    private static IResult Fail(int status, string code, string error) =>
        Results.Json(new BridgeError(false, code, error), BridgeJson.Default.BridgeError, statusCode: status);

    [LoggerMessage(Level = LogLevel.Information, Message = "Read ok in {Milliseconds} ms.")]
    private static partial void LogRead(ILogger logger, long milliseconds);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Read failed: {Reason}.")]
    private static partial void LogFailed(ILogger logger, string reason, Exception exception);
}

/// <summary>The success body of <c>/read</c>.</summary>
internal sealed record ReadResponse(bool Success, MyKadData Data);
