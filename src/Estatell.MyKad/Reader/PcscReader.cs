using PCSC;
using PCSC.Exceptions;

namespace Estatell.MyKad.Reader;

/// <summary>
/// The real reader, through the OS smart-card service (winscard.dll, PCSC.framework).
/// PC/SC contexts should not be shared across threads, so status checks keep one context
/// and each read opens its own.
/// </summary>
public sealed class PcscReader : IReader, IDisposable
{
    // Response data plus SW1 SW2, with room to spare.
    private const int ReceiveBufferSize = 258;

    private readonly ReadGate _gate = new();
    private readonly Lock _statusLock = new();
    private ISCardContext? _statusContext;

    public ReaderStatus GetStatus()
    {
        lock (_statusLock)
        {
            try
            {
                return Find(StatusContext()).Status;
            }
            catch (Exception e) when (IsServiceGone(e))
            {
                // Windows stops the service when the last reader is unplugged. Reopen once.
                ReleaseStatusContext();
                try
                {
                    return Find(StatusContext()).Status;
                }
                catch (Exception again) when (IsServiceGone(again))
                {
                    ReleaseStatusContext();
                    return ReaderStatus.NoReader;
                }
            }
        }
    }

    public IReaderSession Connect(CancellationToken cancellationToken)
    {
        var pass = _gate.Enter(cancellationToken);
        ISCardContext? context = null;
        try
        {
            context = ContextFactory.Instance.Establish(SCardScope.System);
            var found = Find(context);
            if (found.Status.ReaderName is null)
            {
                throw new ReaderException(ReaderErrorCode.NoReader, "No card reader is plugged in.");
            }

            if (!found.Status.CardPresent)
            {
                throw new ReaderException(ReaderErrorCode.NoCard, "No card in the reader.");
            }

            // MyKad only offers T=0. Asking for any protocol fails on the first connect after the card goes in.
            var reader = context.ConnectReader(found.Status.ReaderName, SCardShareMode.Exclusive, SCardProtocol.T0);
            return new Session(context, reader, pass, cancellationToken);
        }
        catch (Exception e)
        {
            context?.Dispose();
            pass.Dispose();
            throw e is ReaderException ? e : Map(e);
        }
    }

    public void Dispose()
    {
        lock (_statusLock)
        {
            ReleaseStatusContext();
        }
    }

    private ISCardContext StatusContext() =>
        _statusContext ??= ContextFactory.Instance.Establish(SCardScope.System);

    private void ReleaseStatusContext()
    {
        try
        {
            _statusContext?.Dispose();
        }
        catch (PCSCException)
        {
            // The service is already gone. Nothing to release.
        }

        _statusContext = null;
    }

    /// <summary>The first reader with a card, else the first reader. Never connects to the card.</summary>
    private static (ReaderStatus Status, byte[]? Atr) Find(ISCardContext context)
    {
        var names = context.GetReaders();
        if (names.Length == 0)
        {
            return (ReaderStatus.NoReader, null);
        }

        var states = context.GetReaderStatus(names);
        try
        {
            var withCard = states.FirstOrDefault(s => s.EventState.HasFlag(SCRState.Present));
            return withCard is null
                ? (new ReaderStatus(names[0], false), null)
                : (new ReaderStatus(withCard.ReaderName, true), withCard.Atr);
        }
        finally
        {
            foreach (var state in states)
            {
                state.Dispose();
            }
        }
    }

    private static bool IsServiceGone(Exception e) =>
        e is NoServiceException or InvalidContextException
        || e is PCSCException { SCardError: SCardError.NoService or SCardError.ServiceStopped or SCardError.InvalidHandle };

    private static ReaderException Map(Exception e)
    {
        var error = e is PCSCException pcsc ? pcsc.SCardError : SCardError.InternalError;
        return error switch
        {
            SCardError.NoService or SCardError.ServiceStopped or SCardError.NoReadersAvailable
                or SCardError.UnknownReader or SCardError.ReaderUnavailable
                => new ReaderException(ReaderErrorCode.NoReader, "No card reader is plugged in.", e),
            SCardError.NoSmartcard or SCardError.RemovedCard
                => new ReaderException(ReaderErrorCode.NoCard, "No card in the reader.", e),
            SCardError.SharingViolation
                => new ReaderException(ReaderErrorCode.Unavailable, "Another app is using the card.", e),
            _ => new ReaderException(ReaderErrorCode.Unavailable, $"The card could not be read ({error}).", e),
        };
    }

    private sealed class Session(ISCardContext context, ICardReader reader, IDisposable pass, CancellationToken cancellationToken)
        : IReaderSession
    {
        private bool _disposed;

        public ResponseApdu Transmit(ReadOnlySpan<byte> command)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            cancellationToken.ThrowIfCancellationRequested();

            var receive = new byte[ReceiveBufferSize];
            try
            {
                var received = reader.Transmit(command.ToArray(), receive);
                if (received < 2)
                {
                    throw new ReaderException(ReaderErrorCode.Unavailable, "The card sent a short response.");
                }

                return new ResponseApdu(receive[..(received - 2)], receive[received - 2], receive[received - 1]);
            }
            catch (PCSCException e)
            {
                throw Map(e);
            }
            finally
            {
                Array.Clear(receive);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            try
            {
                reader.Dispose(); // Disconnects with SCARD_LEAVE_CARD.
                context.Dispose();
            }
            catch (PCSCException)
            {
                // The card or reader is already gone.
            }
            finally
            {
                pass.Dispose();
            }
        }
    }
}
