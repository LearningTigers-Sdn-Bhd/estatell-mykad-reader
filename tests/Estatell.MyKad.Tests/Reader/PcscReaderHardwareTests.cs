using Estatell.MyKad.Reader;

namespace Estatell.MyKad.Tests.Reader;

/// <summary>Needs a real reader with a MyKad in it. Runs only with MYKAD_HARDWARE=1.</summary>
public class PcscReaderHardwareTests
{
    private static readonly byte[] _selectJpn = [0x00, 0xA4, 0x04, 0x00, 0x0A, 0xA0, 0x00, 0x00, 0x00, 0x74, 0x4A, 0x50, 0x4E, 0x00, 0x10];

    private static bool HardwareEnabled => Environment.GetEnvironmentVariable("MYKAD_HARDWARE") == "1";

    [Fact]
    public void ReportsTheReaderAndCard()
    {
        Assert.SkipUnless(HardwareEnabled, "Set MYKAD_HARDWARE=1 with a reader and a MyKad.");
        using var reader = new PcscReader();

        var status = reader.GetStatus();

        Assert.NotNull(status.ReaderName);
        Assert.True(status.CardPresent);
    }

    [Fact]
    public void SelectsTheJpnApplication()
    {
        Assert.SkipUnless(HardwareEnabled, "Set MYKAD_HARDWARE=1 with a reader and a MyKad.");
        using var reader = new PcscReader();

        using (var session = reader.Connect(TestContext.Current.CancellationToken))
        {
            var response = session.Transmit(_selectJpn);
            Assert.Contains(response.Sw1, new byte[] { 0x61, 0x90 });
        }

        // The card is left powered, so a second read connects straight away.
        using var again = reader.Connect(TestContext.Current.CancellationToken);
        Assert.Contains(again.Transmit(_selectJpn).Sw1, new byte[] { 0x61, 0x90 });
    }

    [Fact]
    public void StatusWorksWhileAReadIsOpen()
    {
        Assert.SkipUnless(HardwareEnabled, "Set MYKAD_HARDWARE=1 with a reader and a MyKad.");
        using var reader = new PcscReader();

        using var session = reader.Connect(TestContext.Current.CancellationToken);

        Assert.True(reader.GetStatus().CardPresent);
    }
}
