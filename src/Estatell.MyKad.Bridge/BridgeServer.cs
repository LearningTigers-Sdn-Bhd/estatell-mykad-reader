using Estatell.MyKad.Bridge.Tray;
using Estatell.MyKad.Reader;

namespace Estatell.MyKad.Bridge;

/// <summary>
/// Runs the server beside the tray, and rebuilds it for a restart, because a stopped <see cref="WebApplication"/> cannot start again.
/// While another user's copy holds the port it keeps waiting, so the tray can say so. See docs/02-bridge/04-tray.md#more-than-one-user.
/// </summary>
internal sealed partial class BridgeServer(Func<WebApplication> build, TimeSpan portRetry) : IDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private volatile WebApplication? _app;
    private volatile CancellationTokenSource? _portWait;
    private volatile bool _waitingForPort;
    private volatile bool _stoppingOnPurpose;
    private volatile bool _stopRequested;
    private Action _stopped = () => { };

    /// <summary>Why the server stopped by itself, or null.</summary>
    public Exception? Failure { get; private set; }

    /// <summary>The current app's services. Call <see cref="TryAcquire"/> first.</summary>
    public IServiceProvider Services => _app!.Services;

    /// <summary>Builds the first app. False when another copy of the same user already runs.</summary>
    public bool TryAcquire()
    {
        _app ??= build();
        return _app.Services.GetRequiredService<IInstanceGuard>().TryAcquire();
    }

    /// <summary>Waits for the port and starts the server. Returns once it serves or fails. <paramref name="stopped"/> runs if it stops by itself.</summary>
    public async Task StartAsync(Action stopped)
    {
        _stopped = stopped;
        await _lock.WaitAsync();
        try
        {
            await StartCoreAsync();
        }
        catch (Exception e)
        {
            Fail(e);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RestartAsync()
    {
        _portWait?.Cancel();
        await _lock.WaitAsync();
        try
        {
            _stoppingOnPurpose = true;
            await StopCoreAsync();
            _stoppingOnPurpose = false;

            var app = build();
            _app = app;
            if (!app.Services.GetRequiredService<IInstanceGuard>().TryAcquire())
            {
                throw new InvalidOperationException("Another copy took the lock during the restart.");
            }

            await StartCoreAsync();
        }
        catch (Exception e)
        {
            Fail(e);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task StopAsync()
    {
        _stopRequested = true;
        _portWait?.Cancel();
        await _lock.WaitAsync();
        try
        {
            _stoppingOnPurpose = true;
            await StopCoreAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>The menu's lines. During a restart the old services may be gone, so this reads as no reader for that moment.</summary>
    public TrayStatus Status()
    {
        try
        {
            var services = Services;
            return TrayStatus.Read(
                services.GetRequiredService<IReader>(),
                services.GetRequiredService<ILogger<BridgeStatus>>(),
                services.GetRequiredService<EstatellPresence>(),
                _waitingForPort);
        }
        catch (ObjectDisposedException)
        {
            return TrayStatus.NoReader;
        }
    }

    private async Task StartCoreAsync()
    {
        // A stop can win the lock before a start or a restart that it should cancel.
        if (_stopRequested)
        {
            return;
        }

        var app = _app!;
        var logger = Logger(app);
        using var portWait = new CancellationTokenSource();
        _portWait = portWait;
        try
        {
            await StartupGate.WaitForPortAsync(
                app.Services.GetRequiredService<IInstanceGuard>(),
                portRetry,
                logger,
                portWait.Token,
                waiting: () => _waitingForPort = true);

            app.Lifetime.ApplicationStopping.Register(() =>
            {
                if (!_stoppingOnPurpose)
                {
                    _stopped();
                }
            });
            await app.StartAsync(CancellationToken.None);
            LogStarted(logger, BridgeInfo.Version);
        }
        catch (OperationCanceledException) when (portWait.IsCancellationRequested)
        {
            // A restart or a stop came while it waited for the port.
        }
        finally
        {
            _waitingForPort = false;
            _portWait = null;
        }
    }

    private async Task StopCoreAsync()
    {
        if (_app is not { } app)
        {
            return;
        }

        try
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
        catch (ObjectDisposedException)
        {
            // The test host disposes the app first.
        }
    }

    private void Fail(Exception exception)
    {
        Failure = exception;
        if (_app is { } app)
        {
            try
            {
                var logger = Logger(app);
                LogFailed(logger, exception);
            }
            catch (ObjectDisposedException)
            {
            }
        }

        _stopped();
    }

    public void Dispose() => _lock.Dispose();

    private static ILogger Logger(WebApplication app) =>
        app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Bridge");

    [LoggerMessage(Level = LogLevel.Information, Message = "MyKad Reader {Version} started.")]
    private static partial void LogStarted(ILogger logger, string version);

    [LoggerMessage(Level = LogLevel.Error, Message = "The server stopped.")]
    private static partial void LogFailed(ILogger logger, Exception exception);
}

/// <summary>
/// Replaces the console lifetime. That one catches SIGTERM and Ctrl+C and asks the host to stop, but the tray owns the main
/// thread and nothing waits for the host, so the process would keep running. Program.cs handles the signals instead.
/// </summary>
internal sealed class NoHostLifetime : IHostLifetime
{
    public Task WaitForStartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
