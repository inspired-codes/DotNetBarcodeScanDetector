using Xunit;

namespace InspiredCodes.BarcodeScanDetector.Tests;

/// <summary>
/// What separates a scan from human typing: the gaps between inputs.
/// </summary>
public class TypingTests
{

    [Fact]
    public void HumanTyping_IsNoScan()
    {
        var r = new Recorder();

        r.Type("ABC\r", 0, stepMs: 100);

        Assert.Empty(r.Scans);
    }

    [Fact]
    public void FastBurstWithoutTerminator_IsNoScan()
    {
        var r = new Recorder();

        r.Type("ABCDEF", 0);

        Assert.Empty(r.Scans);
    }

    [Fact]
    public void PauseInsideABurst_StartsTheScanOver()
    {
        var r = new Recorder();

        r.Type("AB", 0);
        r.Type("CD", 200);
        r.Key("\r", 202);

        Assert.Equal(new[] { "CD" }, r.Scans);
    }

    [Theory]
    [InlineData(32, true)]        // a gap of exactly the threshold still counts as fast
    [InlineData(33, false)]
    public void GapAtTheThreshold_IsFast(double stepMs, bool scanned)
    {
        var r = new Recorder();

        r.Type("ABC\r", 0, stepMs);

        Assert.Equal(scanned ? new[] { "ABC" } : new string[0], r.Scans);
    }

    [Fact]
    public void SingleCharacterScan_IsReported()
    {
        var r = new Recorder();

        r.Key("A", 0);
        r.Key("\r", 1);

        Assert.Equal(new[] { "A" }, r.Scans);
    }

}
