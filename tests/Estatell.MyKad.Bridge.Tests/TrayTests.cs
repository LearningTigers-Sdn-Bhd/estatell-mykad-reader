using Estatell.MyKad.Bridge.Tray;

namespace Estatell.MyKad.Bridge.Tests;

public class TrayTests
{
    private static BridgeStatus Reader(string? name, bool card = false) => new(true, "2.0.0", name is not null, name, card);

    [Fact]
    public void AReaderIsReady() =>
        Assert.Equal(
            new TrayStatus(TrayState.Ready, "ACS ACR39U", true, "app.estatell.com"),
            TrayStatus.From(Reader("ACS ACR39U", card: true), "app.estatell.com", waitingForPort: false));

    [Fact]
    public void NoReaderAsksToPlugItIn() =>
        Assert.Equal(TrayState.NoReader, TrayStatus.From(Reader(null), null, waitingForPort: false).State);

    [Fact]
    public void WaitingForThePortWinsOverTheReader() =>
        Assert.Equal(TrayState.OtherUser, TrayStatus.From(Reader("ACS ACR39U"), null, waitingForPort: true).State);

    [Fact]
    public async Task NoTrayRunsUntilStopped()
    {
        var tray = new NoTray();
        var run = Task.Run(() => tray.Run(Actions()), TestContext.Current.CancellationToken);

        tray.Stop();

        await run.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    [Fact]
    public void AStopBeforeTheRunIsNotLost()
    {
        var tray = new NoTray();

        tray.Stop();
        tray.Run(Actions());
    }

    [Fact]
    public void TheNoTrayFlagNeverShowsAnIcon() =>
        Assert.IsType<NoTray>(TrayChooser.Create(BridgeOptions.Parse(["--no-tray"])));

    private static TrayActions Actions() => new(() => { }, () => { }, () => TrayStatus.NoReader);
}
