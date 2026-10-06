using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using InspiredCodes.WPF.BarcodeScanDetector;

namespace InspiredCodes.BarcodeScanDetector.Tests;

/// <summary>
/// The sender of BarcodeScanned: the object passed to a Simulate...FastInput overload for the
/// scan that simulation completes, otherwise null (real input has no sender to report).
/// </summary>
[TestClass()]
public class SenderTests
{

    private int _savedCooldownMillisec;
    private List<object> _bubbleSenders;
    private List<object> _tunnelSenders;
    private EventHandler<BarcodeScannedEventArgs> _bubbleHandler;
    private EventHandler<BarcodeScannedEventArgs> _tunnelHandler;

    [TestInitialize]
    public void InitializeTest()
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

    [TestCleanup]
    public void CleanupTest()
    {
        ScanDetector.BarcodeScanned -= _bubbleHandler;
        ScanDetector.PreviewBarcodeScanned -= _tunnelHandler;
        DetectorConfig.CooldownMillisec = _savedCooldownMillisec;
        ScanDetector.Reset();
    }

    [TestMethod]
    public void SimulateBubbleFastInput_WithSender_PassesItToTheHandler()
    {
        var sender = new object();

        ScanDetector.SimulateBubbleFastInput(sender, "ABC");

        Assert.AreEqual(1, _bubbleSenders.Count);
        Assert.AreSame(sender, _bubbleSenders[0]);
        Assert.AreEqual(0, _tunnelSenders.Count, "only the bubble detector was fed");
    }

    [TestMethod]
    public void SimulateTunnelFastInput_WithSender_PassesItToThePreviewHandler()
    {
        var sender = new object();

        ScanDetector.SimulateTunnelFastInput(sender, "ABC");

        Assert.AreEqual(1, _tunnelSenders.Count);
        Assert.AreSame(sender, _tunnelSenders[0]);
        Assert.AreEqual(0, _bubbleSenders.Count, "only the tunnel detector was fed");
    }

    [TestMethod]
    public void SimulateFastInput_WithoutSender_PassesNull()
    {
        ScanDetector.SimulateBubbleFastInput("ABC");
        ScanDetector.SimulateTunnelFastInput("ABC");

        CollectionAssert.AreEqual(new object[] { null }, _bubbleSenders);
        CollectionAssert.AreEqual(new object[] { null }, _tunnelSenders);
    }

    [TestMethod]
    public void SimulateBubbleFastInput_EachScanGetsItsOwnSender()
    {
        var first = new object();
        var second = new object();

        ScanDetector.SimulateBubbleFastInput(first, "ONE");
        ScanDetector.SimulateBubbleFastInput(second, "TWO");
        ScanDetector.SimulateBubbleFastInput("THREE");

        Assert.AreEqual(3, _bubbleSenders.Count);
        Assert.AreSame(first, _bubbleSenders[0]);
        Assert.AreSame(second, _bubbleSenders[1]);
        Assert.IsNull(_bubbleSenders[2], "a sender must not stick to later scans");
    }

    [TestMethod]
    public void RealInput_AfterASimulationWithSender_HasNoSender()
    {
        ScanDetector.SimulateBubbleFastInput(new object(), "ONE");

        foreach (char c in "TWO\r")
            ScanDetector.ProcessInput(c.ToString());

        Assert.AreEqual(2, _bubbleSenders.Count);
        Assert.IsNull(_bubbleSenders[1]);
    }

}
