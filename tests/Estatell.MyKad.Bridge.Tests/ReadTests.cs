using System.Net;
using System.Text.Json;
using Estatell.MyKad.Reader;
using Estatell.MyKad.Tests.Card;

namespace Estatell.MyKad.Bridge.Tests;

public class ReadTests
{
    private const string Allowed = "https://app.estatell.com";

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    public async Task ReadsTheCardForAnAllowedOrigin(string method)
    {
        using var factory = new BridgeFactory(new CardFixture().Reader());
        using var response = await Send(factory, new HttpMethod(method), Allowed);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await Json(response);
        Assert.True(body.GetProperty("success").GetBoolean());
        var data = body.GetProperty("data");
        Assert.Equal("SITI AMINAH BINTI ABU BAKAR", data.GetProperty("name").GetString());
        Assert.Equal("900101125566", data.GetProperty("ic_number").GetString());
        Assert.Equal("900101-12-5566", data.GetProperty("formatted_ic").GetString());
        Assert.Equal("12", data.GetProperty("state_code").GetString());
        Assert.StartsWith("data:image/jpeg;base64,", data.GetProperty("photo_base64").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RefusesARequestWithoutAnOriginBeforeTouchingTheCard()
    {
        var reader = new CardFixture().Reader();
        using var factory = new BridgeFactory(reader);

        using var response = await Send(factory, HttpMethod.Get, origin: null);

        await AssertError(response, HttpStatusCode.Forbidden, "forbidden");
        Assert.Empty(reader.Sent);
    }

    [Fact]
    public async Task RefusesAForeignOriginBeforeTouchingTheCard()
    {
        var reader = new CardFixture().Reader();
        using var factory = new BridgeFactory(reader);

        using var response = await Send(factory, HttpMethod.Get, "https://evil.com");

        await AssertError(response, HttpStatusCode.Forbidden, "forbidden");
        Assert.Empty(reader.Sent);
    }

    [Fact]
    public async Task ReportsNoReader()
    {
        using var factory = new BridgeFactory(new FakeReader { Status = ReaderStatus.NoReader });
        using var response = await Send(factory, HttpMethod.Get, Allowed);

        await AssertError(response, HttpStatusCode.BadRequest, "no_reader");
    }

    [Fact]
    public async Task ReportsNoCard()
    {
        using var factory = new BridgeFactory(new FakeReader { Status = new ReaderStatus("Fake Reader 0", false) });
        using var response = await Send(factory, HttpMethod.Get, Allowed);

        await AssertError(response, HttpStatusCode.BadRequest, "no_card");
    }

    [Fact]
    public async Task ReportsAFailedCardStepWithoutCardData()
    {
        using var factory = new BridgeFactory(new CardFixture().Reader(failAt: 3));
        using var response = await Send(factory, HttpMethod.Get, Allowed);

        var body = await AssertError(response, HttpStatusCode.InternalServerError, "read_failed");
        Assert.Contains("SW 6A 82", body.GetProperty("error").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReportsBusyWhenTheReaderIsNotFreeBeforeTheTimeout()
    {
        var reader = new StubReader(token =>
        {
            token.WaitHandle.WaitOne();
            throw new ReaderException(ReaderErrorCode.Busy, "Another read is still running.");
        });
        using var factory = new BridgeFactory(reader, readTimeout: TimeSpan.FromMilliseconds(100));

        using var response = await Send(factory, HttpMethod.Get, Allowed);

        await AssertError(response, HttpStatusCode.Conflict, "busy");
    }

    [Fact]
    public async Task ReportsAReadThatRunsPastTheTimeout()
    {
        var reader = new StubReader(token => new HangingSession(token));
        using var factory = new BridgeFactory(reader, readTimeout: TimeSpan.FromMilliseconds(100));

        using var response = await Send(factory, HttpMethod.Get, Allowed);

        await AssertError(response, HttpStatusCode.InternalServerError, "read_failed");
    }

    [Fact]
    public async Task WritesNoCardFieldToTheLog()
    {
        using var factory = new BridgeFactory(new CardFixture().Reader());
        using var ok = await Send(factory, HttpMethod.Get, Allowed);
        var log = await ReadLog(factory, "Read ok");

        Assert.Contains("GET /read -> 200", log, StringComparison.Ordinal);
        AssertNoCardData(log);

        using var failing = new BridgeFactory(new CardFixture().Reader(failAt: 5));
        using var failed = await Send(failing, HttpMethod.Get, Allowed);
        var failedLog = await ReadLog(failing, "Read failed");

        Assert.Contains("Read failed: CardStep", failedLog, StringComparison.Ordinal);
        AssertNoCardData(failedLog);
    }

    private static void AssertNoCardData(string log)
    {
        var card = new CardFixture();
        foreach (var value in new[] { card.Name, card.Ic, "900101", card.Address1, card.Address2, card.City, "88300", "data:image", Convert.ToBase64String(card.Jpeg)[..40] })
        {
            Assert.DoesNotContain(value, log, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static async Task<HttpResponseMessage> Send(BridgeFactory factory, HttpMethod method, string? origin)
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(method, "/read");
        if (origin is not null)
        {
            request.Headers.Add("Origin", origin);
        }

        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement;

    private static async Task<JsonElement> AssertError(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        var body = await Json(response);
        Assert.False(body.GetProperty("success").GetBoolean());
        Assert.Equal(code, body.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("error").GetString()));
        return body;
    }

    /// <summary>The line is written after the response, so wait for the text that proves it landed.</summary>
    private static async Task<string> ReadLog(BridgeFactory factory, string waitFor)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (true)
        {
            var text = File.Exists(factory.LogPath) ? await File.ReadAllTextAsync(factory.LogPath, TestContext.Current.CancellationToken) : "";
            if (text.Contains(waitFor, StringComparison.Ordinal) || DateTime.UtcNow > deadline)
            {
                return text;
            }

            await Task.Delay(20, TestContext.Current.CancellationToken);
        }
    }

    private sealed class StubReader(Func<CancellationToken, IReaderSession> connect) : IReader
    {
        public ReaderStatus GetStatus() => new("Stub Reader", true);

        public IReaderSession Connect(CancellationToken cancellationToken) => connect(cancellationToken);
    }

    private sealed class HangingSession(CancellationToken token) : IReaderSession
    {
        public ResponseApdu Transmit(ReadOnlySpan<byte> command)
        {
            token.WaitHandle.WaitOne();
            token.ThrowIfCancellationRequested();
            return new ResponseApdu([], 0x90, 0x00);
        }

        public void Dispose()
        {
        }
    }
}
