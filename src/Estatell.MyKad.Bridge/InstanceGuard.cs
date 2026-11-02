using System.Net;
using System.Net.Sockets;

namespace Estatell.MyKad.Bridge;

/// <summary>What the bridge checks before it starts serving. See docs/02-bridge/02-security.md#single-instance.</summary>
internal interface IInstanceGuard : IDisposable
{
    /// <summary>True for the first copy of this user. False when another copy of the same user already runs.</summary>
    bool TryAcquire();

    /// <summary>True when nothing listens on the bridge port. A listener here belongs to another user's copy.</summary>
    bool IsPortFree();
}

/// <summary>
/// A lock file for the same user, held until the process ends, and a probe for the port.
/// An exclusive <see cref="FileStream"/> takes an advisory lock on macOS, and the system drops it when the process dies,
/// so a crash never leaves a stale lock. Windows uses a named mutex instead: the handle closes when the process ends.
/// </summary>
internal sealed class InstanceGuard(string? lockPath, int port, string? mutexName = null) : IInstanceGuard
{
    private FileStream? _lock;
    private Mutex? _mutex;

    /// <summary>The user's own folder, so one user's lock never blocks another. Windows uses <see cref="DefaultMutexName"/> instead.</summary>
    public static string? DefaultLockPath() =>
        OperatingSystem.IsMacOS() ? ForMac(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)) : null;

    /// <summary><c>Local\</c> is one per logged-in session, so one user's copy never blocks another's.</summary>
    public static string? DefaultMutexName() => OperatingSystem.IsWindows() ? @"Local\EstatellMyKadBridge" : null;

    public static string ForMac(string home) => Path.Combine(home, "Library", "Application Support", "Estatell", "mykad-bridge.lock");

    public bool TryAcquire()
    {
        if (mutexName is not null)
        {
            return TryAcquireMutex(mutexName);
        }

        if (lockPath is null || _lock is not null)
        {
            return true;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(lockPath)!);
            _lock = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            // No lock is better than no bridge. The port check still stops a second server.
            return true;
        }
    }

    public bool IsPortFree() => IsFree(port);

    public static bool IsFree(int port)
    {
        try
        {
            using var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    public void Dispose()
    {
        _lock?.Dispose();
        _mutex?.Dispose();
    }

    // Only whether the name is new matters, so nothing owns the mutex, and any thread can close it.
    private bool TryAcquireMutex(string name)
    {
        if (_mutex is not null)
        {
            return true;
        }

        var mutex = new Mutex(initiallyOwned: false, name, out var createdNew);
        if (!createdNew)
        {
            mutex.Dispose();
            return false;
        }

        _mutex = mutex;
        return true;
    }
}

internal static partial class StartupGate
{
    /// <summary>Waits while another user's copy holds the port, and checks again every <paramref name="retry"/>.</summary>
    public static async Task WaitForPortAsync(IInstanceGuard guard, TimeSpan retry, ILogger logger, CancellationToken cancellationToken, Action? waiting = null)
    {
        var waited = false;
        while (!guard.IsPortFree())
        {
            if (!waited)
            {
                LogWaiting(logger, BridgeInfo.Port, (int)retry.TotalSeconds);
                waited = true;
                waiting?.Invoke();
            }

            await Task.Delay(retry, cancellationToken);
        }

        if (waited)
        {
            LogPortFree(logger, BridgeInfo.Port);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Port {Port} is taken, probably by another user's copy. Trying again every {Seconds} seconds.")]
    private static partial void LogWaiting(ILogger logger, int port, int seconds);

    [LoggerMessage(Level = LogLevel.Information, Message = "Port {Port} is free.")]
    private static partial void LogPortFree(ILogger logger, int port);
}
