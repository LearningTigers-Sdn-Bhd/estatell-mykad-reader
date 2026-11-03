using System.Net;
using System.Text.Json;
using Estatell.MyKad.Reader;

namespace Estatell.MyKad.Bridge.Tests;

public class StatusTests
{
    [Theory]
    [InlineData("/status")]
    [InlineData("/")]
    [InlineData("/health")]
    public async Task ReturnsTheStatusFieldsOnEachPath(string path)
    {
        using var factory = new BridgeFactory(new FakeReader { Status = new("ACS ACR39U ICC Reader", true) });
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);
        using var body = await ReadJson(response);
        var root = body.RootElement;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            ["online", "version", "reader_connected", "reader_name", "card_present"],
            root.EnumerateObject().Select(p => p.Name));
        Assert.True(root.GetProperty("online").GetBoolean());
        Assert.Equal("2.0.1", root.GetProperty("version").GetString());
        Assert.True(root.GetProperty("reader_connected").GetBoolean());
        Assert.Equal("ACS ACR39U ICC Reader", root.GetProperty("reader_name").GetString());
        Assert.True(root.GetProperty("card_present").GetBoolean());
    }

    [Fact]
    public async Task ReaderWithoutCard()
    {
        using var factory = new BridgeFactory(new FakeReader { Status = new("ACS ACR39U ICC Reader", false) });

        var root = await GetStatus(factory);

        Assert.True(root.GetProperty("reader_connected").GetBoolean());
        Assert.False(root.GetProperty("card_present").GetBoolean());
    }

    [Fact]
    public async Task NoReader()
    {
        using var factory = new BridgeFactory(new FakeReader { Status = ReaderStatus.NoReader });

        var root = await GetStatus(factory);

        Assert.False(root.GetProperty("reader_connected").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("reader_name").ValueKind);
        Assert.False(root.GetProperty("card_present").GetBoolean());
    }

    [Fact]
    public async Task NeverTouchesTheCard()
    {
        var reader = new FakeReader();
        using var factory = new BridgeFactory(reader);

        await GetStatus(factory);

        Assert.Empty(reader.Sent);
    }

    [Fact]
    public async Task SendsNoStoreAndConnectionClose()
    {
        using var factory = new BridgeFactory(new FakeReader());
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/status", TestContext.Current.CancellationToken);

        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.True(response.Headers.ConnectionClose);
    }

    private static async Task<JsonElement> GetStatus(BridgeFactory factory)
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/status", TestContext.Current.CancellationToken);
        using var body = await ReadJson(response);
        return body.RootElement.Clone();
    }

    private static async Task<JsonDocument> ReadJson(HttpResponseMessage response) =>
        await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
            cancellationToken: TestContext.Current.CancellationToken);
}
