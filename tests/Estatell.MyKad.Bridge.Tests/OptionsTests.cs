namespace Estatell.MyKad.Bridge.Tests;

public class OptionsTests
{
    [Fact]
    public void NoFlagsRunsInTheBackground() =>
        Assert.Equal(new BridgeOptions(false, false, false), BridgeOptions.Parse([]));

    [Fact]
    public void ReadsEachFlag() =>
        Assert.Equal(new BridgeOptions(true, true, true), BridgeOptions.Parse(["--verbose", "--version", "--foreground"]));

    [Fact]
    public void IgnoresUnknownFlags() =>
        Assert.Equal(new BridgeOptions(false, true, false), BridgeOptions.Parse(["--no-tray", "--verbose"]));

    [Fact]
    public void VersionDropsTheCommit() =>
        Assert.Equal("2.0.0", BridgeInfo.Version);
}
