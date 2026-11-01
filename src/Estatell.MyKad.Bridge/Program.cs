using System.Net;
using System.Text.Json;
using Estatell.MyKad.Bridge;
using Estatell.MyKad.Reader;

var options = BridgeOptions.Parse(args);
if (options.Version)
{
    Console.WriteLine(BridgeInfo.Version);
    return;
}

// No args to the builder: the flags are ours, not configuration keys.
var builder = WebApplication.CreateSlimBuilder();
builder.WebHost.ConfigureKestrel(kestrel => kestrel.Listen(IPAddress.Loopback, BridgeInfo.Port));
builder.Services.ConfigureHttpJsonOptions(json =>
{
    // The web defaults ask for camel case, and they win over the context's own options.
    json.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    json.SerializerOptions.TypeInfoResolverChain.Insert(0, BridgeJson.Default);
});
builder.Services.AddSingleton<IReader, PcscReader>();
builder.Services.AddSingleton(_ => new OriginPolicy(Environment.GetEnvironmentVariable(OriginPolicy.EnvironmentVariable)));

if (!options.Foreground)
{
    // The file log comes with its own item. Until then the background run is silent.
    builder.Logging.ClearProviders();
}

if (options.Verbose)
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}

var app = builder.Build();

app.Use((context, next) =>
{
    context.Response.Headers.CacheControl = "no-store";
    context.Response.Headers.Connection = "close";
    return next(context);
});

var policy = app.Services.GetRequiredService<OriginPolicy>();
var checksLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(RequestChecks).FullName!);
foreach (var skipped in policy.Skipped)
{
    StartupLog.SkippedOrigin(checksLogger, skipped, OriginPolicy.EnvironmentVariable);
}

app.Use((context, next) => RequestChecks.InvokeAsync(context, next, policy, checksLogger));

app.MapGet("/status", StatusRoute.Get);
app.MapGet("/", StatusRoute.Get);
app.MapGet("/health", StatusRoute.Get);

app.Run();

public partial class Program;

internal static partial class StartupLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Skipped '{Origin}' in {Variable}. Use http(s)://host[:port].")]
    public static partial void SkippedOrigin(ILogger logger, string origin, string variable);
}
