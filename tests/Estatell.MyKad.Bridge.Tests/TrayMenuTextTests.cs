using Estatell.MyKad.Bridge.Tray;

namespace Estatell.MyKad.Bridge.Tests;

public class TrayMenuTextTests
{
    [Fact]
    public void MainLineSaysWhatToDoNext()
    {
        Assert.Equal("🟢 Ready. Insert a MyKad", TrayMenuText.MainLine(new TrayStatus(TrayState.Ready, "ACS ACR39U", false, null)));
        Assert.Equal("🟢 MyKad inserted", TrayMenuText.MainLine(new TrayStatus(TrayState.Ready, "ACS ACR39U", true, null)));
        Assert.Equal("⚪ Plug in the card reader", TrayMenuText.MainLine(TrayStatus.NoReader));
        Assert.Equal("⚪ Open for another user on this computer", TrayMenuText.MainLine(new TrayStatus(TrayState.OtherUser, "ACS ACR39U", true, null)));
    }

    [Fact]
    public void MainWordsHaveNoDot()
    {
        Assert.Equal("Ready. Insert a MyKad", TrayMenuText.MainWords(new TrayStatus(TrayState.Ready, "ACS ACR39U", false, null)));
        Assert.Equal("Plug in the card reader", TrayMenuText.MainWords(TrayStatus.NoReader));
    }

    [Fact]
    public void ReaderLineShowsTheShortName()
    {
        Assert.Equal("Card reader: ACS ACR39U", TrayMenuText.ReaderLine(new TrayStatus(TrayState.Ready, "ACS ACR39U ICC Reader", false, null)));
        Assert.Equal("Card reader: ACS ACR39U", TrayMenuText.ReaderLine(new TrayStatus(TrayState.Ready, "ACS ACR39U ICC Reader 0", false, null)));
        Assert.Equal("Card reader: Other Reader", TrayMenuText.ReaderLine(new TrayStatus(TrayState.Ready, "Other Reader", false, null)));
    }

    [Fact]
    public void ReaderLineNeverShowsAnEmptyName()
    {
        Assert.Equal("Card reader: not found", TrayMenuText.ReaderLine(TrayStatus.NoReader));
        Assert.Equal("Card reader: not found", TrayMenuText.ReaderLine(new TrayStatus(TrayState.Ready, "", false, null)));
        Assert.Equal("Card reader: not found", TrayMenuText.ReaderLine(new TrayStatus(TrayState.Ready, " ICC Reader", false, null)));
    }

    [Fact]
    public void EstatellLineDoesNotShowTheHost()
    {
        Assert.Equal("Estatell: page open", TrayMenuText.EstatellLine(new TrayStatus(TrayState.Ready, "ACS ACR39U", false, "app.estatell.com")));
        Assert.Equal("Estatell: no page open", TrayMenuText.EstatellLine(TrayStatus.NoReader));
    }

    [Fact]
    public void ItemWords()
    {
        Assert.Equal($"Estatell MyKad Reader {BridgeInfo.Version}", TrayMenuText.Header);
        Assert.Equal("Set-up help", TrayMenuText.Help);
        Assert.Equal("Restart MyKad Reader", TrayMenuText.Restart);
    }

    [Fact]
    public void IconIsGreyOnlyWhenNotReady()
    {
        Assert.False(TrayMenuText.IconGrey(TrayState.Ready));
        Assert.True(TrayMenuText.IconGrey(TrayState.NoReader));
        Assert.True(TrayMenuText.IconGrey(TrayState.OtherUser));
    }
}
