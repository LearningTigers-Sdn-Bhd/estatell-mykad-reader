using Estatell.MyKad.Bridge.Tray;
using Estatell.MyKad.Reader;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Estatell.MyKad.Bridge.Tests;

/// <summary>
/// The bridge in a test host, with a <see cref="FakeReader"/> in place of PC/SC.
/// The client sends <c>Host: 127.0.0.1:9292</c>, as a browser on this computer does.
/// </summary>
internal sealed class BridgeFactory : WebApplicationFactory<Program>
{
    private readonly IReader _reader;
    private readonly TimeSpan? _readTimeout;
    private readonly OriginPolicy? _policy;
    private readonly TimeProvider? _clock;
    private readonly string _logDirectory = Path.Combine(Path.GetTempPath(), $"mykad-reader-tests-{Guid.NewGuid():N}");

    public BridgeFactory(IReader reader, OriginPolicy? policy = null, TimeProvider? clock = null, TimeSpan? readTimeout = null)
    {
        _reader = reader;
        _clock = clock;
        _readTimeout = readTimeout;
        _policy = policy;
        ClientOptions.BaseAddress = new Uri("http://127.0.0.1:9292");
    }

    /// <summary>The log file of this host. Never the real one under <c>~/Library/Logs</c>.</summary>
    public string LogPath => Path.Combine(_logDirectory, LogFileOptions.FileName);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_logDirectory))
        {
            Directory.Delete(_logDirectory, recursive: true);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IReader>(_reader);
            services.AddSingleton(new LogFileOptions(LogPath));
            services.AddSingleton<IInstanceGuard>(new FreeGuard());
            services.AddSingleton<ITray>(new NoTray());
            if (_readTimeout is { } timeout)
            {
                services.AddSingleton(new ReadOptions(timeout));
            }

            if (_clock is not null)
            {
                services.AddSingleton(_clock);
            }

            if (_policy is not null)
            {
                services.AddSingleton(_policy);
            }
        });

    /// <summary>Never takes the real lock file or probes the real port.</summary>
    private sealed class FreeGuard : IInstanceGuard
    {
        public bool TryAcquire() => true;

        public bool IsPortFree() => true;

        public void Dispose()
        {
        }
    }
}
