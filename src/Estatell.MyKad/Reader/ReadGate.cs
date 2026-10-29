using System.Diagnostics.CodeAnalysis;

namespace Estatell.MyKad.Reader;

/// <summary>Lets one read run at a time.</summary>
[SuppressMessage("Design", "CA1001", Justification = "SemaphoreSlim only needs disposing when its AvailableWaitHandle is used.")]
internal sealed class ReadGate
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>Waits for the running read to finish. Throws <see cref="ReaderErrorCode.Busy"/> when cancelled first.</summary>
    public IDisposable Enter(CancellationToken cancellationToken)
    {
        try
        {
            _semaphore.Wait(cancellationToken);
        }
        catch (OperationCanceledException e)
        {
            throw new ReaderException(ReaderErrorCode.Busy, "Another read is still running.", e);
        }

        return new Pass(_semaphore);
    }

    private sealed class Pass(SemaphoreSlim semaphore) : IDisposable
    {
        private int _released;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                semaphore.Release();
            }
        }
    }
}
