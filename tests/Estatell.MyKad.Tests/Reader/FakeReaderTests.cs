using Estatell.MyKad.Reader;

namespace Estatell.MyKad.Tests.Reader;

public class FakeReaderTests
{
    private static readonly byte[] _selectJpn = [0x00, 0xA4, 0x04, 0x00, 0x0A, 0xA0, 0x00, 0x00, 0x00, 0x74, 0x4A, 0x50, 0x4E, 0x00, 0x10];
    private static readonly byte[] _readChunk = [0xCC, 0x06, 0x00, 0x00, 0x02];

    [Fact]
    public void ReturnsQueuedResponsesInOrderAndRecordsCommands()
    {
        var reader = new FakeReader()
            .Expect(_selectJpn, [], 0x61, 0x05)
            .Expect(_readChunk, [0x41, 0x42], 0x90, 0x00);

        using var session = reader.Connect(TestContext.Current.CancellationToken);
        var select = session.Transmit(_selectJpn);
        var read = session.Transmit(_readChunk);

        Assert.Equal((0x61, 0x05), (select.Sw1, select.Sw2));
        Assert.Equal([0x41, 0x42], read.Data);
        Assert.Equal([_selectJpn, _readChunk], reader.Sent);
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void UnexpectedCommandThrows()
    {
        var reader = new FakeReader().Expect(_selectJpn, [], 0x61, 0x05);

        using var session = reader.Connect(TestContext.Current.CancellationToken);

        Assert.Throws<InvalidOperationException>(() => session.Transmit(_readChunk));
    }

    [Fact]
    public void NoReaderThrowsNoReader()
    {
        var reader = new FakeReader { Status = ReaderStatus.NoReader };

        var e = Assert.Throws<ReaderException>(() => reader.Connect(TestContext.Current.CancellationToken));

        Assert.Equal(ReaderErrorCode.NoReader, e.Code);
    }

    [Fact]
    public void NoCardThrowsNoCard()
    {
        var reader = new FakeReader { Status = new ReaderStatus("Fake Reader 0", false) };

        var e = Assert.Throws<ReaderException>(() => reader.Connect(TestContext.Current.CancellationToken));

        Assert.Equal(ReaderErrorCode.NoCard, e.Code);
    }

    [Fact]
    public void SecondConnectWaitsThenThrowsBusy()
    {
        var reader = new FakeReader();
        using var first = reader.Connect(TestContext.Current.CancellationToken);
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        var e = Assert.Throws<ReaderException>(() => reader.Connect(timeout.Token));

        Assert.Equal(ReaderErrorCode.Busy, e.Code);
    }

    [Fact]
    public async Task SecondConnectRunsOnceTheFirstIsDisposed()
    {
        var reader = new FakeReader();
        var first = reader.Connect(TestContext.Current.CancellationToken);

        var second = Task.Run(() => reader.Connect(TestContext.Current.CancellationToken), TestContext.Current.CancellationToken);
        await Task.Delay(50, TestContext.Current.CancellationToken);
        Assert.False(second.IsCompleted);

        first.Dispose();
        using var session = await second.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.NotNull(session);
    }

    [Fact]
    public void DisposingTwiceReleasesOnce()
    {
        var reader = new FakeReader();
        var first = reader.Connect(TestContext.Current.CancellationToken);
        first.Dispose();
        first.Dispose();

        using var second = reader.Connect(TestContext.Current.CancellationToken);
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        var e = Assert.Throws<ReaderException>(() => reader.Connect(timeout.Token));
        Assert.Equal(ReaderErrorCode.Busy, e.Code);
    }

    [Fact]
    public void TransmitStopsOnceTheTokenFires()
    {
        var reader = new FakeReader().Expect(_selectJpn, [], 0x61, 0x05);
        using var cancel = new CancellationTokenSource();
        using var session = reader.Connect(cancel.Token);

        cancel.Cancel();

        Assert.Throws<OperationCanceledException>(() => session.Transmit(_selectJpn));
        Assert.Empty(reader.Sent);
    }

    [Fact]
    public void TransmitAfterDisposeThrows()
    {
        var reader = new FakeReader().Expect(_selectJpn, [], 0x61, 0x05);
        var session = reader.Connect(TestContext.Current.CancellationToken);
        session.Dispose();

        Assert.Throws<ObjectDisposedException>(() => session.Transmit(_selectJpn));
    }
}
