using System;
using System.Collections.Generic;
using System.Threading;

using Xunit;

using InspiredCodes.WPF.BarcodeScanDetector;

namespace InspiredCodes.BarcodeScanDetector.Tests;

/// <summary>
/// The configurable timing and buffer limits. They are static, so every test
/// restores them.
/// </summary>
public class DetectorConfigLimitsTests : IDisposable
{

    private long _savedThresholdTicks;
    private int _savedCooldownMillisec;
    private int _savedMaxBufferLength;
    private List<string> _scans;
    private EventHandler<BarcodeScannedEventArgs> _handler;

    public DetectorConfigLimitsTests()
    {
        _savedThresholdTicks = DetectorConfig.ThresholdTicks;
        _savedCooldownMillisec = DetectorConfig.CooldownMillisec;
        _savedMaxBufferLength = DetectorConfig.MaxBufferLength;

        ScanDetector.Reset();
        _scans = new List<string>();
        _handler = (sender, e) => _scans.Add(e.InputText);
        ScanDetector.BarcodeScanned += _handler;
    }

    public void Dispose()
    {
        ScanDetector.BarcodeScanned -= _handler;
        DetectorConfig.ThresholdTicks = _savedThresholdTicks;
        DetectorConfig.CooldownMillisec = _savedCooldownMillisec;
        DetectorConfig.MaxBufferLength = _savedMaxBufferLength;
        ScanDetector.Reset();
    }

    [Fact]
    public void Defaults_AreTheFormerHardCodedValues()
    {
        Assert.Equal(32, DetectorConfig.ThresholdMillisec);
        Assert.Equal(300, DetectorConfig.CooldownMillisec);
        Assert.Equal(4096, DetectorConfig.MaxBufferLength);
    }

    [Fact]
    public void ThresholdMillisec_Setter_UpdatesThresholdTicks()
    {
        DetectorConfig.ThresholdMillisec = 50;

        Assert.Equal(50 * TimeSpan.TicksPerMillisecond, DetectorConfig.ThresholdTicks);
        Assert.Equal(50, DetectorConfig.ThresholdMillisec);
    }

    [Fact]
    public void ThresholdTicks_Setter_UpdatesThresholdMillisec()
    {
        DetectorConfig.ThresholdTicks = 75 * TimeSpan.TicksPerMillisecond;

        Assert.Equal(75, DetectorConfig.ThresholdMillisec);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void ThresholdMillisec_RejectsNegativeValues(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => { DetectorConfig.ThresholdMillisec = value; });

        Assert.Equal(32, DetectorConfig.ThresholdMillisec);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void CooldownMillisec_RejectsNegativeValues(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => { DetectorConfig.CooldownMillisec = value; });

        Assert.Equal(300, DetectorConfig.CooldownMillisec);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void MaxBufferLength_RejectsValuesBelowOne(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => { DetectorConfig.MaxBufferLength = value; });

        Assert.Equal(4096, DetectorConfig.MaxBufferLength);
    }

    [Fact]
    public void CooldownMillisec_AcceptsZero_MeaningNoCooldown()
    {
        DetectorConfig.CooldownMillisec = 0;

        Assert.Equal(0, DetectorConfig.CooldownMillisec);
    }

    [Theory]
    [InlineData(10, true)]
    [InlineData(11, false)]
    public void MaxBufferLength_SetsTheLargestAcceptedScan(int length, bool accepted)
    {
        DetectorConfig.MaxBufferLength = 10;

        ScanDetector.SimulateBubbleFastInput(new string('A', length));

        string[] expected = accepted ? new[] { new string('A', length) } : new string[0];
        Assert.Equal(expected, _scans);
    }

    [Fact]
    public void CooldownMillisec_Zero_AcceptsBackToBackScans()
    {
        DetectorConfig.CooldownMillisec = 0;

        ScanDetector.SimulateBubbleFastInput("ONE");
        ScanDetector.SimulateBubbleFastInput("TWO");

        Assert.Equal(new[] { "ONE", "TWO" }, _scans);
    }

    [Fact]
    public void CooldownMillisec_Shorter_EndsTheCooldownSooner()
    {
        DetectorConfig.CooldownMillisec = 100;

        ScanDetector.SimulateBubbleFastInput("ONE");
        Thread.Sleep(180);  // past 100ms, but well inside the default 300ms
        ScanDetector.SimulateBubbleFastInput("TWO");

        Assert.Equal(new[] { "ONE", "TWO" }, _scans);
    }

    [Fact]
    public void CooldownMillisec_AppliesToTheCooldownStartedByABufferOverflow()
    {
        DetectorConfig.MaxBufferLength = 10;
        DetectorConfig.CooldownMillisec = 100;

        ScanDetector.SimulateBubbleFastInput(new string('A', 11));
        Thread.Sleep(180);  // past 100ms, but well inside the default 300ms
        ScanDetector.SimulateBubbleFastInput("TWO");

        Assert.Equal(new[] { "TWO" }, _scans);
    }

    [Fact]
    public void CooldownMillisec_AppliesToTheExtensionByFastInputDuringTheCooldown()
    {
        DetectorConfig.CooldownMillisec = 100;

        ScanDetector.SimulateBubbleFastInput("ONE");
        Thread.Sleep(20);
        ScanDetector.ProcessInput("Q");  // fast input in the cooldown: extends it to ~+120ms
        Thread.Sleep(160);               // ~+180ms: past 120ms, but well inside 300ms
        foreach (char c in "XYZ")
            ScanDetector.ProcessInput(c.ToString());
        ScanDetector.ProcessInput("\r");

        Assert.Equal(new[] { "ONE", "XYZ" }, _scans);
    }

    [Fact]
    public void CooldownMillisec_Longer_KeepsDiscardingScansLonger()
    {
        DetectorConfig.CooldownMillisec = 600;

        ScanDetector.SimulateBubbleFastInput("ONE");
        Thread.Sleep(350);  // past the default 300ms, but well inside 600ms
        ScanDetector.SimulateBubbleFastInput("TWO");

        Assert.Equal(new[] { "ONE" }, _scans);
    }

}
