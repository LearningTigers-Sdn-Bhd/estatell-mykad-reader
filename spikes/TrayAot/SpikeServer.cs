using System.Net;
using System.Text.Json.Serialization;

namespace TrayAot;

/// <summary>A Kestrel server that can be stopped and started again in the same process.</summary>
internal sealed class SpikeServer(int port)
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private WebApplication? _app;
    private int _starts;
    private volatile string _state = "stopped";

    public string Url => $"http://127.0.0.1:{port}";

    public string StatusLine => _state;

    public async Task StartAsync()
    {
        await _lock.WaitAsync();
        try
        {
            await StartCoreAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RestartAsync()
    {
        await _lock.WaitAsync();
        try
        {
            _state = "restarting";
            await StopCoreAsync();
            await StartCoreAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task StopAsync()
    {
        await _lock.WaitAsync();
        try
        {
            await StopCoreAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task StartCoreAsync()
    {
        var start = ++_starts;
        var startedAt = DateTimeOffset.Now;

        var builder = WebApplication.CreateSlimBuilder();
        builder.Services.AddSingleton<IHostLifetime, TrayLifetime>();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, port));
        builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.TypeInfoResolverChain.Insert(0, SpikeJson.Default));

        var app = builder.Build();
        app.MapGet("/status", () => new SpikeStatus("ok", start, startedAt, Environment.ProcessId));
        await app.StartAsync();

        _app = app;
        _state = $"running · start {start}";
    }

    private async Task StopCoreAsync()
    {
        if (_app is null)
        {
            return;
        }

        await _app.StopAsync();
        await _app.DisposeAsync();
        _app = null;
        _state = "stopped";
    }
}

/// <summary>
/// Replaces the console lifetime. That one catches SIGTERM and Ctrl+C and asks the host to stop, but the tray owns the main
/// thread and nothing waits for the host, so the process would keep running. Program.cs handles the signals instead.
/// </summary>
internal sealed class TrayLifetime : IHostLifetime
{
    public Task WaitForStartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed record SpikeStatus(string Status, int Start, DateTimeOffset StartedAt, int ProcessId);

[JsonSerializable(typeof(SpikeStatus))]
internal sealed partial class SpikeJson : JsonSerializerContext;
