namespace Estatell.MyKad.Bridge.Tests;

public class OptionsTests
{
    [Fact]
    public void NoFlagsRunsInTheBackground() =>
        Assert.Equal(new BridgeOptions(false, false, false, false), BridgeOptions.Parse([]));

    [Fact]
    public void ReadsEachFlag() =>
        Assert.Equal(new BridgeOptions(true, true, true, true), BridgeOptions.Parse(["--verbose", "--version", "--foreground", "--no-tray"]));

    [Fact]
    public void IgnoresUnknownFlags() =>
        Assert.Equal(new BridgeOptions(false, true, false, false), BridgeOptions.Parse(["--unknown", "--verbose"]));

    [Fact]
    public void VersionDropsTheCommit() =>
        Assert.Equal("2.0.1", BridgeInfo.Version);
}
