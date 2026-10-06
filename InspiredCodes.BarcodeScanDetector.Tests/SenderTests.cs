using System;
using System.Collections.Generic;

using Xunit;

using InspiredCodes.WPF.BarcodeScanDetector;

namespace InspiredCodes.BarcodeScanDetector.Tests;

/// <summary>
/// The sender of BarcodeScanned: the object passed to a Simulate...FastInput overload for the
/// scan that simulation completes, otherwise null (real input has no sender to report).
/// </summary>
public class SenderTests : IDisposable
{

    private int _savedCooldownMillisec;
    private List<object> _bubbleSenders;
    private List<object> _tunnelSenders;
    private EventHandler<BarcodeScannedEventArgs> _bubbleHandler;
    private EventHandler<BarcodeScannedEventArgs> _tunnelHandler;

    public SenderTests()
    {
        _savedCooldownMillisec = DetectorConfig.CooldownMillisec;
        // back-to-back scans without waiting out the cooldown
        DetectorConfig.CooldownMillisec = 0;

        ScanDetector.Reset();
        _bubbleSenders = new List<object>();
        _tunnelSenders = new List<object>();
        _bubbleHandler = (sender, e) => _bubbleSenders.Add(sender);
        _tunnelHandler = (sender, e) => _tunnelSenders.Add(sender);
        ScanDetector.BarcodeScanned += _bubbleHandler;
        ScanDetector.PreviewBarcodeScanned += _tunnelHandler;
    }

    public void Dispose()
    {
        ScanDetector.BarcodeScanned -= _bubbleHandler;
        ScanDetector.PreviewBarcodeScanned -= _tunnelHandler;
        DetectorConfig.CooldownMillisec = _savedCooldownMillisec;
        ScanDetector.Reset();
    }

    [Fact]
    public void SimulateBubbleFastInput_WithSender_PassesItToTheHandler()
    {
        var sender = new object();

        ScanDetector.SimulateBubbleFastInput(sender, "ABC");

        Assert.Same(sender, Assert.Single(_bubbleSenders));
        Assert.True(_tunnelSenders.Count == 0, "only the bubble detector was fed");
    }

    [Fact]
    public void SimulateTunnelFastInput_WithSender_PassesItToThePreviewHandler()
    {
        var sender = new object();

        ScanDetector.SimulateTunnelFastInput(sender, "ABC");

        Assert.Same(sender, Assert.Single(_tunnelSenders));
        Assert.True(_bubbleSenders.Count == 0, "only the tunnel detector was fed");
    }

    [Fact]
    public void SimulateFastInput_WithoutSender_PassesNull()
    {
        ScanDetector.SimulateBubbleFastInput("ABC");
        ScanDetector.SimulateTunnelFastInput("ABC");

        Assert.Equal(new object[] { null }, _bubbleSenders);
        Assert.Equal(new object[] { null }, _tunnelSenders);
    }

    [Fact]
    public void SimulateBubbleFastInput_EachScanGetsItsOwnSender()
    {
        var first = new object();
        var second = new object();

        ScanDetector.SimulateBubbleFastInput(first, "ONE");
        ScanDetector.SimulateBubbleFastInput(second, "TWO");
        ScanDetector.SimulateBubbleFastInput("THREE");

        Assert.Equal(3, _bubbleSenders.Count);
        Assert.Same(first, _bubbleSenders[0]);
        Assert.Same(second, _bubbleSenders[1]);
        Assert.Null(_bubbleSenders[2]); // a sender must not stick to later scans
    }

    [Fact]
    public void RealInput_AfterASimulationWithSender_HasNoSender()
    {
        ScanDetector.SimulateBubbleFastInput(new object(), "ONE");

        foreach (char c in "TWO\r")
            ScanDetector.ProcessInput(c.ToString());

        Assert.Equal(2, _bubbleSenders.Count);
        Assert.Null(_bubbleSenders[1]);
    }

}
