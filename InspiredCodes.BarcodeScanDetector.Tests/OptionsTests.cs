using System;

using Xunit;

namespace InspiredCodes.BarcodeScanDetector.Tests;

/// <summary>
/// R7: defaults and validation of the options; OD-4: an engine works with a snapshot of them.
/// </summary>
public class OptionsTests
{

    [Fact]
    public void Defaults()
    {
        var options = new ScanDetectorOptions();

        Assert.Equal(TimeSpan.FromMilliseconds(32), options.InterKeyThreshold);
        Assert.Equal(TimeSpan.FromMilliseconds(300), options.Cooldown);
        Assert.Equal(4096, options.MaxLength);
        Assert.Equal(ScanTerminators.Enter, options.Terminators);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(long.MinValue)]
    public void NegativeTimes_AreRejected_AndLeaveTheOldValue(long ticks)
    {
        var options = new ScanDetectorOptions();

        Assert.Throws<ArgumentOutOfRangeException>(() => { options.InterKeyThreshold = TimeSpan.FromTicks(ticks); });
        Assert.Throws<ArgumentOutOfRangeException>(() => { options.Cooldown = TimeSpan.FromTicks(ticks); });

        Assert.Equal(TimeSpan.FromMilliseconds(32), options.InterKeyThreshold);
        Assert.Equal(TimeSpan.FromMilliseconds(300), options.Cooldown);
    }

    [Fact]
    public void ZeroTimes_AreAllowed()
    {
        var options = new ScanDetectorOptions { InterKeyThreshold = TimeSpan.Zero, Cooldown = TimeSpan.Zero };

        Assert.Equal(TimeSpan.Zero, options.InterKeyThreshold);
        Assert.Equal(TimeSpan.Zero, options.Cooldown);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void MaxLength_BelowOne_IsRejected_AndLeavesTheOldValue(int value)
    {
        var options = new ScanDetectorOptions();

        Assert.Throws<ArgumentOutOfRangeException>(() => { options.MaxLength = value; });

        Assert.Equal(4096, options.MaxLength);
    }

    [Fact]
    public void MaxLength_OfOne_IsAllowed()
    {
        Assert.Equal(1, new ScanDetectorOptions { MaxLength = 1 }.MaxLength);
    }

    [Theory]
    [InlineData(ScanTerminators.None)]
    [InlineData((ScanTerminators)4)]
    [InlineData(ScanTerminators.Enter | (ScanTerminators)8)]
    public void Terminators_NoneOrUndefined_AreRejected(ScanTerminators value)
    {
        var options = new ScanDetectorOptions();

        Assert.Throws<ArgumentOutOfRangeException>(() => { options.Terminators = value; });

        Assert.Equal(ScanTerminators.Enter, options.Terminators);
    }

    [Theory]
    [InlineData(100, true)]       // keys 50 ms apart count as fast
    [InlineData(32, false)]       // with the default they are human typing
    public void InterKeyThreshold_DecidesWhatIsFast(double thresholdMs, bool scanned)
    {
        var r = new Recorder(new ScanDetectorOptions { InterKeyThreshold = Recorder.Ms(thresholdMs) });

        r.Type("ABC\r", 0, stepMs: 50);

        Assert.Equal(scanned ? new[] { "ABC" } : new string[0], r.Scans);
    }

    [Fact]
    public void ChangingTheOptionsAfterwards_DoesNotAffectTheEngine()
    {
        var options = new ScanDetectorOptions();
        var r = new Recorder(options);

        options.Cooldown = TimeSpan.Zero;
        options.MaxLength = 1;
        r.Type("ABC", 0);
        r.Key("\r", 3);
        r.Probe(10);

        // still a 300 ms cooldown, still room for 3 characters
        Assert.Equal(new[] { "ABC" }, r.Scans);
    }

}
