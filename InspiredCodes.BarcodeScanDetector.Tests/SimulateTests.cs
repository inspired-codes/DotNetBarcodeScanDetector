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
