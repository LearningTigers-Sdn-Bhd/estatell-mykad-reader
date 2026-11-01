using System.Net;
using Estatell.MyKad.Reader;
using Microsoft.Extensions.DependencyInjection;

namespace Estatell.MyKad.Bridge.Tests;

public class EstatellPresenceTests
{
    [Fact]
    public void IsNotSeenBeforeAnyRequest()
    {
        var presence = new EstatellPresence(new FakeClock());

        Assert.Null(presence.ConnectedHost);
    }

    [Fact]
    public void IsConnectedForTenSecondsAfterTheLastRequest()
    {
        var clock = new FakeClock();
        var presence = new EstatellPresence(clock);
        presence.Record("https://app.estatell.com");

        clock.Advance(TimeSpan.FromSeconds(10));
        Assert.Equal("app.estatell.com", presence.ConnectedHost);

        clock.Advance(TimeSpan.FromMilliseconds(1));
        Assert.Null(presence.ConnectedHost);
    }

    [Fact]
    public void AnotherRequestExtendsTheWindowAndNamesTheNewHost()
    {
        var clock = new FakeClock();
        var presence = new EstatellPresence(clock);
        presence.Record("https://app.estatell.com");
        clock.Advance(TimeSpan.FromSeconds(8));
        presence.Record("https://demo.estatell.com");
        clock.Advance(TimeSpan.FromSeconds(8));

        Assert.Equal("demo.estatell.com", presence.ConnectedHost);
    }

    [Fact]
    public async Task AnAllowedOriginIsRecorded()
    {
        var (factory, presence) = Create();
        using var _ = factory;
        using var client = factory.CreateClient();

        using var response = await Get(client, "https://demo.estatell.com");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("demo.estatell.com", presence.ConnectedHost);
    }

    [Fact]
    public async Task APreflightFromAnAllowedOriginIsRecorded()
    {
        var (factory, presence) = Create();
        using var _ = factory;
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/status");
        request.Headers.Add("Origin", "https://app.estatell.com");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("app.estatell.com", presence.ConnectedHost);
    }

    [Fact]
    public async Task ARefusedOriginIsNotRecorded()
    {
        var (factory, presence) = Create();
        using var _ = factory;
        using var client = factory.CreateClient();

        using var response = await Get(client, "https://evil.com");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(presence.ConnectedHost);
    }

    [Fact]
    public async Task ARequestWithoutAnOriginIsNotRecorded()
    {
        var (factory, presence) = Create();
        using var _ = factory;
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/status", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(presence.ConnectedHost);
    }

    private static (BridgeFactory Factory, EstatellPresence Presence) Create()
    {
        var factory = new BridgeFactory(new FakeReader(), clock: new FakeClock());
        return (factory, factory.Services.GetRequiredService<EstatellPresence>());
    }

    private static Task<HttpResponseMessage> Get(HttpClient client, string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/status");
        request.Headers.Add("Origin", origin);
        return client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private sealed class FakeClock : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }
}
