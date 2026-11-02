using System.Net;
using Estatell.MyKad.Bridge.Tray;
using Estatell.MyKad.Reader;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Estatell.MyKad.Bridge.Tests;

public class BridgeServerTests
{
    private static readonly TimeSpan Retry = TimeSpan.FromMilliseconds(5);

    [Fact]
    public async Task ServesOnceStarted()
    {
        var apps = new Apps();
        using var server = new BridgeServer(apps.Build, Retry);

        Assert.True(server.TryAcquire());
        await server.StartAsync(() => { });

        Assert.Equal(HttpStatusCode.OK, await GetAsync(apps.Last));
        await server.StopAsync();
    }

    [Fact]
    public void ASecondCopyOfTheSameUserIsRefused()
    {
        var apps = new Apps { Guard = new Guard { Acquires = false } };

        using var server = new BridgeServer(apps.Build, Retry);

        Assert.False(server.TryAcquire());
    }

    [Fact]
    public async Task RestartStopsTheOldServerAndStartsANewOne()
    {
        var apps = new Apps();
        var stopped = 0;
        using var server = new BridgeServer(apps.Build, Retry);
        server.TryAcquire();
        await server.StartAsync(() => stopped++);
        var firstLifetime = apps.Last.Lifetime;

        await server.RestartAsync();

        Assert.Equal(2, apps.Count);
        Assert.True(firstLifetime.ApplicationStopped.IsCancellationRequested);
        Assert.Equal(HttpStatusCode.OK, await GetAsync(apps.Last));
        Assert.Equal(0, stopped);
        await server.StopAsync();
    }

    [Fact]
    public async Task ShowsTheOtherUserStateWhileThePortIsTaken()
    {
        var guard = new Guard { PortFree = false };
        using var server = new BridgeServer(new Apps { Guard = guard }.Build, Retry);
        server.TryAcquire();

        var start = server.StartAsync(() => { });
        await WaitUntilAsync(() => server.Status().State == TrayState.OtherUser);

        guard.PortFree = true;
        await start;

        Assert.NotEqual(TrayState.OtherUser, server.Status().State);
        await server.StopAsync();
    }

    [Fact]
    public async Task StopEndsTheWaitForThePort()
    {
        using var server = new BridgeServer(new Apps { Guard = new Guard { PortFree = false } }.Build, Retry);
        server.TryAcquire();
        var start = server.StartAsync(() => { });
        await WaitUntilAsync(() => server.Status().State == TrayState.OtherUser);

        await server.StopAsync();
        await start;

        Assert.Null(server.Failure);
    }

    [Fact]
    public async Task AStopBeforeTheStartKeepsItFromServing()
    {
        var apps = new Apps();
        using var server = new BridgeServer(apps.Build, Retry);
        server.TryAcquire();
        var lifetime = apps.Last.Lifetime;

        await server.StopAsync();
        await server.StartAsync(() => { });

        Assert.False(lifetime.ApplicationStarted.IsCancellationRequested);
        Assert.Null(server.Failure);
    }

    [Fact]
    public async Task RestartEndsTheWaitForThePort()
    {
        var guard = new Guard { PortFree = false };
        var apps = new Apps { Guard = guard };
        using var server = new BridgeServer(apps.Build, Retry);
        server.TryAcquire();
        var start = server.StartAsync(() => { });
        await WaitUntilAsync(() => server.Status().State == TrayState.OtherUser);

        var restart = server.RestartAsync();
        await start;
        await WaitUntilAsync(() => apps.Count == 2);
        guard.PortFree = true;
        await restart;

        Assert.Equal(HttpStatusCode.OK, await GetAsync(apps.Last));
        await server.StopAsync();
    }

    [Fact]
    public async Task TellsTheTrayWhenTheServerStopsByItself()
    {
        var apps = new Apps();
        using var stopped = new SemaphoreSlim(0);
        using var server = new BridgeServer(apps.Build, Retry);
        server.TryAcquire();
        await server.StartAsync(() => stopped.Release());

        apps.Last.Lifetime.StopApplication();

        Assert.True(await stopped.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        await server.StopAsync();
    }

    [Fact]
    public async Task AFailedStartIsReportedAndTellsTheTray()
    {
        var stopped = 0;
        using var server = new BridgeServer(new Apps { FailToStart = true }.Build, Retry);
        server.TryAcquire();

        await server.StartAsync(() => stopped++);

        Assert.NotNull(server.Failure);
        Assert.Equal(1, stopped);
    }

    [Fact]
    public async Task StatusReadsAsNoReaderOnceTheServicesAreGone()
    {
        using var server = new BridgeServer(new Apps().Build, Retry);
        server.TryAcquire();
        await server.StartAsync(() => { });
        await server.StopAsync();

        Assert.Equal(TrayStatus.NoReader, server.Status());
    }

    private static async Task<HttpStatusCode> GetAsync(WebApplication app)
    {
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient();
        using var response = await client.GetAsync($"{address}/ping", TestContext.Current.CancellationToken);
        return response.StatusCode;
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition())
        {
            await Task.Delay(1, timeout.Token);
        }
    }

    private sealed class Apps
    {
        public Guard Guard { get; init; } = new();

        public bool FailToStart { get; init; }

        public int Count { get; private set; }

        public WebApplication Last { get; private set; } = null!;

        public WebApplication Build()
        {
            var builder = WebApplication.CreateSlimBuilder();
            builder.WebHost.ConfigureKestrel(kestrel => kestrel.Listen(IPAddress.Loopback, 0));
            builder.Services.AddSingleton<IHostLifetime, NoHostLifetime>();
            builder.Services.AddSingleton<IInstanceGuard>(Guard);
            builder.Services.AddSingleton<IReader>(new FakeReader());
            builder.Services.AddSingleton(new EstatellPresence(TimeProvider.System));
            if (FailToStart)
            {
                builder.Services.AddSingleton<IHostedService, FailingService>();
            }

            var app = builder.Build();
            app.MapGet("/ping", () => "ok");
            Last = app;
            Count++;
            return app;
        }
    }

    private sealed class FailingService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken) => throw new InvalidOperationException("No start.");

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class Guard : IInstanceGuard
    {
        public bool Acquires { get; init; } = true;

        public volatile bool PortFree = true;

        public bool TryAcquire() => Acquires;

        public bool IsPortFree() => PortFree;

        public void Dispose()
        {
        }
    }
}
