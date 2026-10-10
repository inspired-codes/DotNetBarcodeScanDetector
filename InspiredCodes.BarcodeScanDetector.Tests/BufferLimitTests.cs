using Xunit;

namespace InspiredCodes.BarcodeScanDetector.Tests;

/// <summary>
/// R5: a scan of exactly MaxLength characters is reported; a longer one is discarded, raises no
/// event (never an empty one) and starts the cooldown.
/// </summary>
public class BufferLimitTests
{

    private const int DefaultMaxLength = 4096;

    [Fact]
    public void ScanAtTheDefaultLimit_IsReportedWithAllCharacters()
    {
        var r = new Recorder();

        double last = r.Type(new string('A', DefaultMaxLength), 0);
        r.Key("\r", last + 1);

        Assert.Equal(DefaultMaxLength, Assert.Single(r.Scans).Length);
    }

    [Theory]
    [InlineData(DefaultMaxLength + 1)]
    [InlineData(DefaultMaxLength + 2)]
    public void ScanOverTheDefaultLimit_RaisesNoEvent(int length)
    {
        var r = new Recorder();

        double last = r.Type(new string('A', length), 0);
        r.Key("\r", last + 1);

        Assert.Empty(r.Scans);
    }

    [Theory]
    [InlineData(DefaultMaxLength + 1, 100, false)]
    [InlineData(DefaultMaxLength + 1, 300, true)]
    [InlineData(DefaultMaxLength + 2, 100, false)]
    [InlineData(DefaultMaxLength + 2, 300, true)]
    public void ScanOverTheDefaultLimit_StartsTheCooldown(int length, double probeAfter, bool accepted)
    {
        var r = new Recorder();
        double last = r.Type(new string('A', length), 0);
        double terminatorAt = last + 1;
        r.Key("\r", terminatorAt);    // the cooldown ends 300 ms after this

        r.Probe(terminatorAt + probeAfter);

        Assert.Equal(accepted ? new[] { "XYZ" } : new string[0], r.Scans);
    }

    [Theory]
    [InlineData(10, true)]
    [InlineData(11, false)]
    public void MaxLength_IsTheLongestReportedScan(int length, bool accepted)
    {
        var r = new Recorder(new ScanDetectorOptions { MaxLength = 10 });

        double last = r.Type(new string('A', length), 0);
        r.Key("\r", last + 1);

        Assert.Equal(accepted ? new[] { new string('A', length) } : new string[0], r.Scans);
    }

    [Theory]
    [InlineData(60, false)]
    [InlineData(111, true)]
    public void OverflowCooldown_LastsTheConfiguredCooldown(double probeAt, bool accepted)
    {
        var r = new Recorder(new ScanDetectorOptions { MaxLength = 10, Cooldown = Recorder.Ms(100) });
        r.Type(new string('A', 11), 0);
        r.Key("\r", 11);              // overflows here: cooldown until 111

        r.Probe(probeAt);

        Assert.Equal(accepted ? new[] { "XYZ" } : new string[0], r.Scans);
    }

}
