using System;
using System.Collections.Generic;

using Xunit;

namespace InspiredCodes.BarcodeScanDetector.Tests;

/// <summary>
/// The static <see cref="ScanDetector"/> facade over the process-wide Default and Preview engines.
/// Its engines are shared by every test, so each test resets them and unsubscribes in Dispose.
/// </summary>
public class FacadeTests : IDisposable
{

    private readonly List<(object Sender, string Text)> _bubble = new List<(object, string)>();
    private readonly List<(object Sender, string Text)> _tunnel = new List<(object, string)>();
    private readonly EventHandler<BarcodeScannedEventArgs> _bubbleHandler;
    private readonly EventHandler<BarcodeScannedEventArgs> _tunnelHandler;

    public FacadeTests()
    {
        ScanDetector.Reset();
        _bubbleHandler = (sender, e) => _bubble.Add((sender, e.InputText));
        _tunnelHandler = (sender, e) => _tunnel.Add((sender, e.InputText));
        ScanDetector.BarcodeScanned += _bubbleHandler;
        ScanDetector.PreviewBarcodeScanned += _tunnelHandler;
    }

    public void Dispose()
    {
        ScanDetector.BarcodeScanned -= _bubbleHandler;
        ScanDetector.PreviewBarcodeScanned -= _tunnelHandler;
        ScanDetector.Reset();
    }

    [Fact]
    public void SimulateBubbleFastInput_IsReportedByTheDefaultEngine()
    {
        ScanDetector.SimulateBubbleFastInput("ABC");

        var scan = Assert.Single(_bubble);
        Assert.Equal("ABC", scan.Text);
        Assert.Same(ScanDetector.Default, scan.Sender);
        Assert.Empty(_tunnel);
    }

    [Fact]
    public void SimulateTunnelFastInput_IsReportedByThePreviewEngine()
    {
        ScanDetector.SimulateTunnelFastInput("ABC");

        var scan = Assert.Single(_tunnel);
        Assert.Equal("ABC", scan.Text);
        Assert.Same(ScanDetector.Preview, scan.Sender);
        Assert.Empty(_bubble);
    }

    [Fact]
    public void ProcessInput_FeedsTheDefaultEngine_AndProcessPreviewInput_ThePreviewEngine()
    {
        foreach (char c in "ONE\r")
            ScanDetector.ProcessInput(c.ToString());
        foreach (char c in "TWO\r")
            ScanDetector.ProcessPreviewInput(c.ToString());

        Assert.Equal("ONE", Assert.Single(_bubble).Text);
        Assert.Equal("TWO", Assert.Single(_tunnel).Text);
    }

    [Fact]
    public void Reset_EndsTheCooldownOfBothEngines()
    {
        ScanDetector.SimulateBubbleFastInput("ONE");
        ScanDetector.SimulateTunnelFastInput("ONE");

        ScanDetector.Reset();
        ScanDetector.SimulateBubbleFastInput("TWO");
        ScanDetector.SimulateTunnelFastInput("TWO");

        Assert.Equal(2, _bubble.Count);
        Assert.Equal(2, _tunnel.Count);
    }

    [Fact]
    public void Unsubscribing_StopsTheEvents()
    {
        ScanDetector.BarcodeScanned -= _bubbleHandler;

        ScanDetector.SimulateBubbleFastInput("ABC");

        Assert.Empty(_bubble);
    }

}
