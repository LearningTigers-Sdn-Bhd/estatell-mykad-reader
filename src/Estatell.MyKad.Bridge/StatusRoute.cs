using System.Text.Json.Serialization;
using Estatell.MyKad.Reader;

namespace Estatell.MyKad.Bridge;

internal static partial class StatusRoute
{
    /// <summary>Never touches the card. A failed status check reads as no reader.</summary>
    public static BridgeStatus Get(IReader reader, ILogger<BridgeStatus> logger)
    {
        ReaderStatus status;
        try
        {
            status = reader.GetStatus();
        }
        catch (Exception e)
        {
            LogStatusFailed(logger, e);
            status = ReaderStatus.NoReader;
        }

        return new BridgeStatus(
            Online: true,
            Version: BridgeInfo.Version,
            ReaderConnected: status.ReaderName is not null,
            ReaderName: status.ReaderName,
            CardPresent: status.CardPresent);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "The reader status check failed.")]
    private static partial void LogStatusFailed(ILogger logger, Exception exception);
}

/// <summary>The <c>GET /status</c> body. The 1.x names stay, so a 1.x Estatell page still shows the status.</summary>
internal sealed record BridgeStatus(
    bool Online,
    string Version,
    bool ReaderConnected,
    string? ReaderName,
    bool CardPresent);

/// <summary>Every failure body. <c>Error</c> is for logs and developers and never holds card data.</summary>
internal sealed record BridgeError(bool Success, string Code, string Error);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(BridgeStatus))]
[JsonSerializable(typeof(BridgeError))]
internal sealed partial class BridgeJson : JsonSerializerContext;
