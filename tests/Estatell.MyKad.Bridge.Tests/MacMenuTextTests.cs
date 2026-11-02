using Estatell.MyKad.Bridge.Tray;

namespace Estatell.MyKad.Bridge.Tests;

public class MacMenuTextTests
{
    [Fact]
    public void StatusLineUsesTheWordsOfTheDocs()
    {
        Assert.Equal("● Ready", MacMenuText.StatusLine(TrayState.Ready));
        Assert.Equal("○ Plug in the card reader", MacMenuText.StatusLine(TrayState.NoReader));
        Assert.Equal("○ MyKad Reader is open for another user", MacMenuText.StatusLine(TrayState.OtherUser));
    }

    [Fact]
    public void ShowsAllLinesForAReaderWithACardAndAnEstatellPage()
    {
        var status = new TrayStatus(TrayState.Ready, "ACS ACR39U", true, "app.estatell.com");

        Assert.Equal("Card reader: ACS ACR39U", MacMenuText.ReaderLine(status));
        Assert.Equal("Card: inserted", MacMenuText.CardLine(status));
        Assert.Equal("Estatell: connected · app.estatell.com", MacMenuText.EstatellLine(status));
    }

    [Fact]
    public void ShowsNotFoundNoneAndNotSeenWhenThereIsNothing()
    {
        var status = TrayStatus.NoReader;

        Assert.Equal("Card reader: not found", MacMenuText.ReaderLine(status));
        Assert.Equal("Card: none", MacMenuText.CardLine(status));
        Assert.Equal("Estatell: not seen", MacMenuText.EstatellLine(status));
    }

    [Fact]
    public void HeaderHasTheVersion() => Assert.Equal($"MyKad Reader {BridgeInfo.Version}", MacMenuText.Header);

    [Fact]
    public void IconIsGreyWithoutAReader()
    {
        Assert.False(MacMenuText.IconGrey(TrayState.Ready));
        Assert.True(MacMenuText.IconGrey(TrayState.NoReader));
        Assert.True(MacMenuText.IconGrey(TrayState.OtherUser));
    }

    [Fact]
    public void NotSeenDoesNotChangeTheIcon() =>
        Assert.False(MacMenuText.IconGrey(new TrayStatus(TrayState.Ready, "ACS ACR39U", false, null).State));
}
