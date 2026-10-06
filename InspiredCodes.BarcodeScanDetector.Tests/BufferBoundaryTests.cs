using System;
using System.Collections.Generic;
using System.Threading;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using InspiredCodes.WPF.BarcodeScanDetector;

namespace InspiredCodes.BarcodeScanDetector.Tests;

/// <summary>
/// Scans around the 4096 character buffer limit, and other cases where a newline
/// arrives with nothing to report.
/// </summary>
[TestClass()]
public class BufferBoundaryTests
{

    private const int MaxLength = 4096;

    private List<string> _scans;
    private EventHandler<BarcodeScannedEventArgs> _handler;

    [TestInitialize]
    public void InitializeTest()
    {
        ScanDetector.Reset();
        _scans = new List<string>();
        _handler = (sender, e) => _scans.Add(e.InputText);
        ScanDetector.BarcodeScanned += _handler;
    }

    [TestCleanup]
    public void CleanupTest()
    {
        ScanDetector.BarcodeScanned -= _handler;
        ScanDetector.Reset();
    }

    [TestMethod]
    public void ScanAtTheLimit_RaisesOneEventWithAllCharacters()
    {
        ScanDetector.SimulateBubbleFastInput(new string('A', MaxLength));

        Assert.AreEqual(1, _scans.Count);
        Assert.AreEqual(MaxLength, _scans[0].Length);
    }

    [DataTestMethod]
    [DataRow(MaxLength + 1)]
    [DataRow(MaxLength + 2)]
    public void ScanOverTheLimit_RaisesNoEvent(int length)
    {
        ScanDetector.SimulateBubbleFastInput(new string('A', length));

        CollectionAssert.AreEqual(new string[0], _scans);
    }

    [DataTestMethod]
    [DataRow(MaxLength + 1)]
    [DataRow(MaxLength + 2)]
    public void ScanOverTheLimit_StartsCooldownThatExpires(int length)
    {
        ScanDetector.SimulateBubbleFastInput(new string('A', length));

        // discarded: the overflow started a cooldown
        ScanDetector.SimulateBubbleFastInput("DURING");
        CollectionAssert.AreEqual(new string[0], _scans);

        // the cooldown ends on its own, after which scanning works again
        Thread.Sleep(350);
        ScanDetector.SimulateBubbleFastInput("AFTER");
        CollectionAssert.AreEqual(new[] { "AFTER" }, _scans);
    }

    [TestMethod]
    public void NewlineWithNothingBuffered_RaisesNoEventAndStartsNoCooldown()
    {
        // Enter arrives fast after a reset, with no text before it
        ScanDetector.ProcessInput("\r");
        CollectionAssert.AreEqual(new string[0], _scans);

        // there was no scan, so there must be no cooldown swallowing the next one
        foreach (char c in "ABC")
            ScanDetector.ProcessInput(c.ToString());
        ScanDetector.ProcessInput("\r");

        CollectionAssert.AreEqual(new[] { "ABC" }, _scans);
    }

}
