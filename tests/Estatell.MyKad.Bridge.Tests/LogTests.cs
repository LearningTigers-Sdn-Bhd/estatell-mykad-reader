using System.Net;
using Estatell.MyKad.Reader;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Estatell.MyKad.Bridge.Tests;

public class LogTests
{
    [Fact]
    public void TheMacPathIsTheOneX1Used() =>
        Assert.Equal("/Users/aina/Library/Logs/estatell-mykad-bridge.log", LogFileOptions.ForMac("/Users/aina"));

    [Fact]
    public async Task RotatesAtTheSizeAndKeepsTwoOldFiles()
    {
        var directory = Directory.CreateTempSubdirectory("mykad-log-");
        try
        {
            var path = Path.Combine(directory.FullName, "bridge.log");
            var writer = new RotatingFileWriter(path, maxBytes: 120, oldFiles: 2);

            for (var i = 1; i <= 8; i++)
            {
                writer.WriteLine($"line {i} " + new string('x', 40));
            }

            Assert.Equal(["bridge.log", "bridge.log.1", "bridge.log.2"], Directory.GetFiles(directory.FullName).Select(Path.GetFileName).Order());
            foreach (var file in Directory.GetFiles(directory.FullName))
            {
                Assert.True(new FileInfo(file).Length <= 120, file);
            }

            // Newest in the base file, oldest kept in .2, anything older gone.
            Assert.Contains("line 8", await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
            Assert.Contains("line 3", await File.ReadAllTextAsync(path + ".2", TestContext.Current.CancellationToken));
            Assert.DoesNotContain("line 2", string.Concat(Directory.GetFiles(directory.FullName).Select(File.ReadAllText)));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task LogsARefusedRequestWithItsPathStatusAndOrigin()
    {
        using var factory = new BridgeFactory(new FakeReader());
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/status");
        request.Headers.Add("Origin", "https://evil.com");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var log = await ReadLog(factory, "403");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("INFO  Requests GET /status -> 403 origin=https://evil.com", log);
    }

    [Fact]
    public async Task NeverLogsTheQueryStringOrAForgedLine()
    {
        using var factory = new BridgeFactory(new FakeReader());
        using var client = factory.CreateClient();

        await client.GetAsync("/x%0D%0A2026-01-01%20INFO%20Forged?name=AINA%20BINTI%20ALI&ic=900101-14-5678", TestContext.Current.CancellationToken);
        var log = await ReadLog(factory, "404");

        Assert.DoesNotContain("AINA", log);
        Assert.DoesNotContain("900101", log);
        Assert.DoesNotContain(log.Split('\n'), line => line.StartsWith("2026-01-01", StringComparison.Ordinal));
    }

    [Fact]
    public async Task KeepsASuccessfulStatusPollOutOfTheLogUnlessVerbose()
    {
        using var factory = new BridgeFactory(new FakeReader());
        using var client = factory.CreateClient();
        await client.GetAsync("/status", TestContext.Current.CancellationToken);

        // A request after the poll marks the end: both were logged in order, or neither was.
        await client.GetAsync("/nothing", TestContext.Current.CancellationToken);
        var log = await ReadLog(factory, "/nothing");

        Assert.DoesNotContain("GET /status", log);

        using var verbose = factory.WithWebHostBuilder(builder => builder.ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Debug)));
        using var verboseClient = verbose.CreateClient();
        await verboseClient.GetAsync("/status", TestContext.Current.CancellationToken);

        Assert.Contains("DEBUG Requests GET /status -> 200", await ReadLog(factory, "DEBUG Requests GET /status"));
    }

    /// <summary>The line is written after the response body, so wait for the text that proves it landed.</summary>
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
}
