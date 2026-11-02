using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Estatell.MyKad.Bridge.Tray;
using Estatell.MyKad.Reader;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;

namespace Estatell.MyKad.Bridge;

/// <summary>Builds the server. A restart calls this again, because a stopped app cannot start again.</summary>
internal static partial class BridgeApp
{
    public static WebApplication Build(BridgeOptions options)
    {
        // No args to the builder: the flags are ours, not configuration keys.
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.ConfigureKestrel(kestrel => kestrel.Listen(IPAddress.Loopback, BridgeInfo.Port));
        builder.WebHost.UseSockets(sockets => sockets.CreateBoundListenSocket = BindListenSocket);
        builder.Services.ConfigureHttpJsonOptions(json =>
        {
            // The web defaults ask for camel case, and they win over the context's own options.
            json.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
            json.SerializerOptions.TypeInfoResolverChain.Insert(0, BridgeJson.Default);
        });
        builder.Services.AddSingleton<IHostLifetime, NoHostLifetime>();
        builder.Services.AddSingleton<ITray>(_ => TrayChooser.Create(options));
        builder.Services.AddSingleton<IReader, PcscReader>();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<EstatellPresence>();
        builder.Services.AddSingleton(ReadOptions.Default);
        builder.Services.AddSingleton<IInstanceGuard>(_ => new InstanceGuard(InstanceGuard.DefaultLockPath(), BridgeInfo.Port, InstanceGuard.DefaultMutexName()));
        builder.Services.AddSingleton(_ => new OriginPolicy(
            Environment.GetEnvironmentVariable(OriginPolicy.EnvironmentVariable),
#if DEBUG
            allowDevelopmentHosts: true));
#else
            allowDevelopmentHosts: false));
#endif

        if (!options.Foreground)
        {
            builder.Logging.ClearProviders();
        }

        // The file log is always on. --foreground adds the console, --verbose adds Debug lines.
        builder.Services.AddSingleton(new LogFileOptions(LogFileOptions.Default()));
        builder.Services.AddSingleton<ILoggerProvider, FileLoggerProvider>();
        builder.Logging.AddFilter("Microsoft", LogLevel.Warning);

        if (options.Verbose)
        {
            builder.Logging.SetMinimumLevel(LogLevel.Debug);
        }

        var app = builder.Build();

        var requestLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Requests");

        app.Use((context, next) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers.Connection = "close";
            return next(context);
        });

        app.Use((context, next) => RequestLog.InvokeAsync(context, next, requestLogger));

        var policy = app.Services.GetRequiredService<OriginPolicy>();
        var presence = app.Services.GetRequiredService<EstatellPresence>();
        var checksLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(RequestChecks).FullName!);
        foreach (var skipped in policy.Skipped)
        {
            LogSkippedOrigin(checksLogger, skipped, OriginPolicy.EnvironmentVariable);
        }

        app.Use((context, next) => RequestChecks.InvokeAsync(context, next, policy, presence, checksLogger));

        app.MapGet("/status", StatusRoute.Get);
        app.MapGet("/", StatusRoute.Get);
        app.MapGet("/health", StatusRoute.Get);
        app.MapMethods("/read", ["GET", "POST"], ReadRoute.Handle);

        return app;
    }

    /// <summary>On Windows, SO_EXCLUSIVEADDRUSE stops another program from sharing the port. It must be set before the bind.</summary>
    private static Socket BindListenSocket(EndPoint endPoint)
    {
        if (!OperatingSystem.IsWindows())
        {
            return SocketTransportOptions.CreateDefaultBoundListenSocket(endPoint);
        }

        var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { ExclusiveAddressUse = true };
        try
        {
            socket.Bind(endPoint);
            return socket;
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Skipped '{Origin}' in {Variable}. Use http(s)://host[:port].")]
    private static partial void LogSkippedOrigin(ILogger logger, string origin, string variable);
}
