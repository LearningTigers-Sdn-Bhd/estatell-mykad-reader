using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TrayAot;

// M1 spike: a Kestrel server on a background thread and a native tray icon, in one Native AOT binary.
// --no-tray runs the server only. --self-test (macOS) clicks the menu from code. --console (Windows) opens a console for the log, like --foreground will.
// The main thread stays synchronous: AppKit must run on it, and an await could move us off it.
if (OperatingSystem.IsWindows() && args.Contains("--console"))
{
    _ = Win32.AllocConsole();
}

Console.WriteLine($"Runtime: {RuntimeInformation.RuntimeIdentifier}, AOT: {!RuntimeFeature.IsDynamicCodeSupported}");

var server = new SpikeServer(9293);
server.StartAsync().GetAwaiter().GetResult();
Console.WriteLine($"Listening on {server.Url}");

var actions = new TrayActions(
    Open: () => Process.Start(new ProcessStartInfo($"{server.Url}/status") { UseShellExecute = true })?.Dispose(),
    Restart: () => _ = Task.Run(async () =>
    {
        try
        {
            await server.RestartAsync();
            Console.WriteLine($"Restarted: {server.StatusLine}");
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }
    }),
    Quit: () =>
    {
        server.StopAsync().GetAwaiter().GetResult();
        Environment.Exit(0);
    },
    StatusLine: () => server.StatusLine);

// SIGTERM comes from launchctl, an installer upgrade or logout. Stop the server, then exit.
using var sigterm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, _ => actions.Quit());
using var sigint = PosixSignalRegistration.Create(PosixSignal.SIGINT, _ => actions.Quit());

if (args.Contains("--no-tray"))
{
    Thread.Sleep(Timeout.Infinite);
}
else if (OperatingSystem.IsWindows())
{
    WindowsTray.Run(actions);
}
else if (OperatingSystem.IsMacOS())
{
    MacTray.Run(actions, args.Contains("--self-test"));
}
else
{
    Console.Error.WriteLine("No tray on this OS. Use --no-tray.");
    return 1;
}

return 0;
