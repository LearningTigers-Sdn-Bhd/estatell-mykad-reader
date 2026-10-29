namespace Estatell.MyKad.Reader;

/// <summary>
/// A reader for tests. Set <see cref="Status"/>, queue the expected commands with <see cref="Expect"/>,
/// and check <see cref="Sent"/>. A command that was not expected next throws.
/// </summary>
public sealed class FakeReader : IReader
{
    private readonly ReadGate _gate = new();
    private readonly Queue<(byte[] Command, ResponseApdu Response)> _expected = new();
    private readonly List<byte[]> _sent = [];

    public ReaderStatus Status { get; set; } = new("Fake Reader 0", true);

    public IReadOnlyList<byte[]> Sent => _sent;

    public int Remaining => _expected.Count;

    public FakeReader Expect(byte[] command, byte[] data, byte sw1, byte sw2)
    {
        _expected.Enqueue((command, new ResponseApdu(data, sw1, sw2)));
        return this;
    }

    public ReaderStatus GetStatus() => Status;

    public IReaderSession Connect(CancellationToken cancellationToken)
    {
        if (Status.ReaderName is null)
        {
            throw new ReaderException(ReaderErrorCode.NoReader, "No card reader is plugged in.");
        }

        if (!Status.CardPresent)
        {
            throw new ReaderException(ReaderErrorCode.NoCard, "No card in the reader.");
        }

        return new Session(this, _gate.Enter(cancellationToken), cancellationToken);
    }

    private sealed class Session(FakeReader reader, IDisposable pass, CancellationToken cancellationToken) : IReaderSession
    {
        private bool _disposed;

        public ResponseApdu Transmit(ReadOnlySpan<byte> command)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            cancellationToken.ThrowIfCancellationRequested();

            var sent = command.ToArray();
            reader._sent.Add(sent);

            if (!reader._expected.TryDequeue(out var next) || !next.Command.AsSpan().SequenceEqual(sent))
            {
                throw new InvalidOperationException($"Unexpected command {Convert.ToHexString(sent)}.");
            }

            return next.Response;
        }

        public void Dispose()
        {
            _disposed = true;
            pass.Dispose();
        }
    }
}
