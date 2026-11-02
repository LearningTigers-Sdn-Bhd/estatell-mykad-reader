using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging.Abstractions;

namespace Estatell.MyKad.Bridge.Tests;

public class InstanceGuardTests
{
    [Fact]
    public void TheMacLockIsInTheUsersOwnFolder() =>
        Assert.Equal("/Users/aina/Library/Application Support/Estatell/mykad-bridge.lock", InstanceGuard.ForMac("/Users/aina"));

    [Fact]
    public void OnlyTheFirstCopyOfAUserTakesTheLock()
    {
        using var directory = new TempDirectory();
        var path = Path.Combine(directory.Path, "nested", "mykad-bridge.lock");
        using var first = new InstanceGuard(path, port: 1);
        using var second = new InstanceGuard(path, port: 1);

        Assert.True(first.TryAcquire());
        Assert.True(first.TryAcquire());
        Assert.False(second.TryAcquire());
    }

    [Fact]
    public void TheLockIsFreeAgainWhenTheFirstCopyEnds()
    {
        using var directory = new TempDirectory();
        var path = Path.Combine(directory.Path, "mykad-bridge.lock");
        var first = new InstanceGuard(path, port: 1);
        Assert.True(first.TryAcquire());
        first.Dispose();

        using var second = new InstanceGuard(path, port: 1);

        Assert.True(second.TryAcquire());
    }

    [Fact]
    public void OnlyTheFirstCopyTakesTheMutexUntilItEnds()
    {
        var name = $"EstatellMyKadTest{Guid.NewGuid():N}";
        var first = new InstanceGuard(lockPath: null, port: 1, name);
        using var second = new InstanceGuard(lockPath: null, port: 1, name);

        Assert.True(first.TryAcquire());
        Assert.True(first.TryAcquire());
        Assert.False(second.TryAcquire());

        first.Dispose();

        Assert.True(second.TryAcquire());
    }

    [Fact]
    public void WithoutALockPathEveryCopyMayStart()
    {
        using var first = new InstanceGuard(lockPath: null, port: 1);
        using var second = new InstanceGuard(lockPath: null, port: 1);

        Assert.True(first.TryAcquire());
        Assert.True(second.TryAcquire());
    }

    [Fact]
    public void SeesAPortThatAnotherCopyHolds()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        Assert.False(InstanceGuard.IsFree(port));

        listener.Stop();

        Assert.True(InstanceGuard.IsFree(port));
    }

    [Fact]
    public async Task WaitsForThePortAndGoesOnOnceItIsFree()
    {
        var guard = new BusyThenFree(busyChecks: 3);

        await StartupGate.WaitForPortAsync(guard, TimeSpan.FromMilliseconds(1), NullLogger.Instance, TestContext.Current.CancellationToken);

        Assert.Equal(4, guard.Checks);
    }

    [Fact]
    public async Task DoesNotWaitWhenThePortIsFree()
    {
        var guard = new BusyThenFree(busyChecks: 0);

        await StartupGate.WaitForPortAsync(guard, TimeSpan.FromMilliseconds(1), NullLogger.Instance, TestContext.Current.CancellationToken);

        Assert.Equal(1, guard.Checks);
    }

    [Fact]
    public async Task StopsWaitingWhenCancelled()
    {
        using var cancel = new CancellationTokenSource(50);
        var guard = new BusyThenFree(busyChecks: int.MaxValue);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => StartupGate.WaitForPortAsync(guard, TimeSpan.FromMilliseconds(5), NullLogger.Instance, cancel.Token));
    }

    private sealed class BusyThenFree(int busyChecks) : IInstanceGuard
    {
        public int Checks { get; private set; }

        public bool TryAcquire() => true;

        public bool IsPortFree() => ++Checks > busyChecks;

        public void Dispose()
        {
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = Directory.CreateTempSubdirectory("mykad-lock-").FullName;

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
