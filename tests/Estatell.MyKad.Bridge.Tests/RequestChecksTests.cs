using System.Net;
using System.Text.Json;
using Estatell.MyKad.Reader;
using Microsoft.AspNetCore.Http;

namespace Estatell.MyKad.Bridge.Tests;

public class RequestChecksTests
{
    [Theory]
    [InlineData("127.0.0.1:9292")]
    [InlineData("localhost:9292")]
    [InlineData("LOCALHOST:9292")]
    public async Task AllowsTheLocalHosts(string host)
    {
        using var factory = new BridgeFactory(new FakeReader());
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/status");
        request.Headers.Host = host;

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("evil.com")]
    [InlineData("evil.com:9292")]
    [InlineData("127.0.0.1")]
    [InlineData("127.0.0.1:80")]
    [InlineData("localhost")]
    [InlineData("[::1]:9292")]
    public async Task RefusesOtherHosts(string host)
    {
        var reader = new FakeReader();
        using var factory = new BridgeFactory(reader);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/status");
        request.Headers.Host = host;

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        await AssertForbidden(response);
    }

    [Fact]
    public async Task RefusesAMissingHost()
    {
        using var factory = new BridgeFactory(new FakeReader());

        var context = await factory.Server.SendAsync(
            c =>
            {
                c.Request.Method = HttpMethods.Get;
                c.Request.Path = "/status";
                c.Request.Host = default;
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    [Fact]
    public async Task AllowsNoOriginWithoutCorsHeaders()
    {
        using var factory = new BridgeFactory(new FakeReader());
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/status", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Theory]
    [InlineData("https://app.estatell.com")]
    [InlineData("https://demo.estatell.com")]
    [InlineData("http://localhost:5173")]
    [InlineData("https://estatell.test")]
    [InlineData("https://pms.example.com")]
    public async Task AllowsKnownOrigins(string origin)
    {
        using var factory = new BridgeFactory(new FakeReader(), new OriginPolicy("https://pms.example.com", allowDevelopmentHosts: true));
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/status");
        request.Headers.Add("Origin", origin);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([origin], response.Headers.GetValues("Access-Control-Allow-Origin"));
        Assert.Equal(["Origin"], response.Headers.Vary);
        Assert.True(response.Headers.CacheControl?.NoStore);
    }

    [Theory]
    [InlineData("http://evil.com")]
    [InlineData("https://app.estatell.com.evil.com")]
    [InlineData("https://foo.test.evil.com")]
    [InlineData("null")]
    public async Task RefusesOtherOrigins(string origin)
    {
        var reader = new FakeReader();
        using var factory = new BridgeFactory(reader);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/status");
        request.Headers.Add("Origin", origin);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        await AssertForbidden(response);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task AnswersThePreflight()
    {
        using var factory = new BridgeFactory(new FakeReader());
        using var client = factory.CreateClient();
        using var request = Preflight("https://app.estatell.com");
        request.Headers.Add("Access-Control-Request-Private-Network", "true");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(["https://app.estatell.com"], response.Headers.GetValues("Access-Control-Allow-Origin"));
        Assert.Equal(["GET, POST, OPTIONS"], response.Headers.GetValues("Access-Control-Allow-Methods"));
        Assert.Equal(["Content-Type"], response.Headers.GetValues("Access-Control-Allow-Headers"));
        Assert.Equal(["600"], response.Headers.GetValues("Access-Control-Max-Age"));
        Assert.Equal(["true"], response.Headers.GetValues("Access-Control-Allow-Private-Network"));
    }

    [Fact]
    public async Task SendsPrivateNetworkOnlyWhenAsked()
    {
        using var factory = new BridgeFactory(new FakeReader());
        using var client = factory.CreateClient();
        using var request = Preflight("https://app.estatell.com");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Private-Network"));
    }

    [Fact]
    public async Task RefusesThePreflightOfOtherOrigins()
    {
        using var factory = new BridgeFactory(new FakeReader());
        using var client = factory.CreateClient();
        using var request = Preflight("http://evil.com");
        request.Headers.Add("Access-Control-Request-Private-Network", "true");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        await AssertForbidden(response);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Private-Network"));
    }

    private static HttpRequestMessage Preflight(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/read");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");
        return request;
    }

    private static async Task AssertForbidden(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);

        using var body = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(body.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("forbidden", body.RootElement.GetProperty("code").GetString());
        Assert.NotEmpty(body.RootElement.GetProperty("error").GetString()!);
    }
}
