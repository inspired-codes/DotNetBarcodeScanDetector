using System;
using System.Collections.Generic;

using Xunit;

using InspiredCodes.BarcodeScanDetector.Tests.Mock;

namespace InspiredCodes.BarcodeScanDetector.Tests;

/// <summary>
/// Simulate: a scan typed at the current time of the engine's own clock, without waiting.
/// </summary>
public class SimulateTests
{

    private static (ScanDetectorEngine Engine, List<string> Scans) Create(ScanDetectorOptions options = null)
    {
        var engine = new ScanDetectorEngine(options);
        var scans = new List<string>();
        engine.BarcodeScanned += (sender, e) => scans.Add(e.InputText);
        return (engine, scans);
    }

    [Fact]
    public void RealWorldBarcodes_AreReportedUnchanged()
    {
        // no cooldown, so the barcodes can follow each other immediately
        var (engine, scans) = Create(new ScanDetectorOptions { Cooldown = TimeSpan.Zero });
        var barcodes = new SomeBarcodes();

        foreach (string barcode in barcodes)
            engine.Simulate(barcode);

        Assert.Equal(barcodes, scans);
    }

    [Theory]
    [InlineData("ABC\r")]
    [InlineData("ABC\n")]
    [InlineData("ABC\r\n")]
    public void TrailingEnter_IsIgnored(string barcode)
    {
        var (engine, scans) = Create();

        engine.Simulate(barcode);

        Assert.Equal(new[] { "ABC" }, scans);
    }

    [Fact]
    public void Null_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new ScanDetectorEngine().Simulate(null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("\r\n")]
    [InlineData("A\rB")]
    [InlineData("A\nB")]
    [InlineData("\rABC")]
    public void EmptyOrContainingEnter_IsRejected(string barcode)
    {
        Assert.Throws<ArgumentException>(() => new ScanDetectorEngine().Simulate(barcode));
    }

    [Fact]
    public void WithTimestamp_IsReportedAtThatTime()
    {
        var r = new Recorder();

        r.Engine.Simulate("ABC", Recorder.Ms(1234.5));

        var scan = Assert.Single(r.Events);
        Assert.Equal("ABC", scan.InputText);
        Assert.Equal(Recorder.Ms(1234.5), scan.Timestamp);
    }

    [Theory]
    [InlineData(100, false)]      // inside the cooldown of the scan at 3 ms
    [InlineData(303, true)]       // after it
    public void WithTimestamp_SharesTheCallerTimeline(double simulateAt, bool accepted)
    {
        var r = new Recorder();
        r.Type("ABC", 0);
        r.Key("\r", 3);

        r.Engine.Simulate("XYZ", Recorder.Ms(simulateAt));

        Assert.Equal(accepted ? new[] { "ABC", "XYZ" } : new[] { "ABC" }, r.Scans);
    }

    [Fact]
    public void WithTimestamp_ValidatesLikeTheOwnClockVersion()
    {
        var engine = new ScanDetectorEngine();

        Assert.Throws<ArgumentNullException>(() => engine.Simulate(null, TimeSpan.Zero));
        Assert.Throws<ArgumentException>(() => engine.Simulate("\r\n", TimeSpan.Zero));
        Assert.Throws<ArgumentException>(() => engine.Simulate("A\rB", TimeSpan.Zero));
    }

    [Fact]
    public void WithTabTerminator_EndsTheScanWithTab()
    {
        var (engine, scans) = Create(new ScanDetectorOptions { Terminators = ScanTerminators.Tab });

        engine.Simulate("ABC\t");

        Assert.Equal(new[] { "ABC" }, scans);
    }

    [Fact]
    public void WithTabTerminator_TabInsideIsRejected_ButEnterIsOrdinaryText()
    {
        var (engine, scans) = Create(new ScanDetectorOptions { Terminators = ScanTerminators.Tab });

        Assert.Throws<ArgumentException>(() => engine.Simulate("A\tB"));
        engine.Simulate("A\rB");

        Assert.Equal(new[] { "A\rB" }, scans);
    }

}
