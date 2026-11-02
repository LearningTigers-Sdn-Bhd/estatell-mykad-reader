using System.Runtime.InteropServices;
using Estatell.MyKad.Bridge;
using Estatell.MyKad.Bridge.Tray;

// The main thread stays synchronous: AppKit must run on it, and an await could move us off it.
var options = BridgeOptions.Parse(args);
if (options.Version)
{
    Console.WriteLine(BridgeInfo.Version);
    return 0;
}

using var server = new BridgeServer(() => BridgeApp.Build(options), TimeSpan.FromSeconds(30));

// A second copy of the same user exits with 0, so launchd does not restart it.
if (!server.TryAcquire())
{
    var startupLogger = server.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Bridge");
    StartupLog.AlreadyRunning(startupLogger);
    return 0;
}

var tray = server.Services.GetRequiredService<ITray>();
var trayLogger = server.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Tray");

// SIGTERM comes from launchctl, an installer upgrade or logout. See docs/02-bridge/04-tray.md#signals.
void OnSignal(PosixSignalContext context)
{
    context.Cancel = true;
    tray.Stop();
}

using var sigterm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, OnSignal);
using var sigint = PosixSignalRegistration.Create(PosixSignal.SIGINT, OnSignal);

var serverStart = Task.Run(() => server.StartAsync(tray.Stop));

tray.Run(new TrayActions(
    Help: () => TrayHelp.Open(trayLogger),
    Restart: () => _ = Task.Run(server.RestartAsync),
    Status: server.Status));

// Stop first: it ends a wait for the port, which the start would otherwise block on.
server.StopAsync().GetAwaiter().GetResult();
serverStart.GetAwaiter().GetResult();
return server.Failure is null ? 0 : 1;

public partial class Program;

internal static partial class StartupLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "MyKad Reader is already running for this user. Exiting.")]
    public static partial void AlreadyRunning(ILogger logger);
}
