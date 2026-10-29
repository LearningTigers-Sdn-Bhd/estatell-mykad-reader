namespace Estatell.MyKad.Reader;

/// <summary>The only way the card logic reaches a card reader.</summary>
public interface IReader
{
    /// <summary>Which reader is used and whether a card is in it. Never connects to the card.</summary>
    ReaderStatus GetStatus();

    /// <summary>
    /// Opens an exclusive session for one read. Waits while another read is running,
    /// and throws <see cref="ReaderErrorCode.Busy"/> if <paramref name="cancellationToken"/> fires first.
    /// The token also stops the session between commands.
    /// </summary>
    IReaderSession Connect(CancellationToken cancellationToken);
}

/// <summary>One exclusive connection to the card. Dispose to disconnect and leave the card powered.</summary>
public interface IReaderSession : IDisposable
{
    /// <summary>Sends one APDU. Throws <see cref="OperationCanceledException"/> once the session's token has fired.</summary>
    ResponseApdu Transmit(ReadOnlySpan<byte> command);
}

/// <param name="ReaderName">The reader with a card, else the first reader, else null.</param>
/// <param name="CardPresent">True when that reader holds a card.</param>
public sealed record ReaderStatus(string? ReaderName, bool CardPresent)
{
    public static readonly ReaderStatus NoReader = new(null, false);
}

/// <summary>A card response, split into its data and the two status words.</summary>
public sealed record ResponseApdu(byte[] Data, byte Sw1, byte Sw2);

public enum ReaderErrorCode
{
    NoReader,
    NoCard,
    Busy,
    Unavailable,
}

/// <summary>A reader problem. The message never holds card data.</summary>
public sealed class ReaderException(ReaderErrorCode code, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public ReaderErrorCode Code { get; } = code;
}
