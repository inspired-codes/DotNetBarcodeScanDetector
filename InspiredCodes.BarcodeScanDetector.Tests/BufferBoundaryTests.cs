using System;
using System.Collections.Generic;
using System.Threading;

using Xunit;

using InspiredCodes.WPF.BarcodeScanDetector;

namespace InspiredCodes.BarcodeScanDetector.Tests;

/// <summary>
/// Scans around the 4096 character buffer limit, and other cases where a newline
/// arrives with nothing to report.
/// </summary>
public class BufferBoundaryTests : IDisposable
{

    private const int MaxLength = 4096;

    private List<string> _scans;
    private EventHandler<BarcodeScannedEventArgs> _handler;

    public BufferBoundaryTests()
    {
        ScanDetector.Reset();
        _scans = new List<string>();
        _handler = (sender, e) => _scans.Add(e.InputText);
        ScanDetector.BarcodeScanned += _handler;
    }

    public void Dispose()
    {
        ScanDetector.BarcodeScanned -= _handler;
        ScanDetector.Reset();
    }

    [Fact]
    public void ScanAtTheLimit_RaisesOneEventWithAllCharacters()
    {
        ScanDetector.SimulateBubbleFastInput(new string('A', MaxLength));

        Assert.Equal(MaxLength, Assert.Single(_scans).Length);
    }

    [Theory]
    [InlineData(MaxLength + 1)]
    [InlineData(MaxLength + 2)]
    public void ScanOverTheLimit_RaisesNoEvent(int length)
    {
        ScanDetector.SimulateBubbleFastInput(new string('A', length));

        Assert.Empty(_scans);
    }

    [Theory]
    [InlineData(MaxLength + 1)]
    [InlineData(MaxLength + 2)]
    public void ScanOverTheLimit_StartsCooldownThatExpires(int length)
    {
        ScanDetector.SimulateBubbleFastInput(new string('A', length));

        // discarded: the overflow started a cooldown
        ScanDetector.SimulateBubbleFastInput("DURING");
        Assert.Empty(_scans);

        // the cooldown ends on its own, after which scanning works again
        Thread.Sleep(350);
        ScanDetector.SimulateBubbleFastInput("AFTER");
        Assert.Equal(new[] { "AFTER" }, _scans);
    }

    [Fact]
    public void NewlineWithNothingBuffered_RaisesNoEventAndStartsNoCooldown()
    {
        // Enter arrives fast after a reset, with no text before it
        ScanDetector.ProcessInput("\r");
        Assert.Empty(_scans);

        // there was no scan, so there must be no cooldown swallowing the next one
        foreach (char c in "ABC")
            ScanDetector.ProcessInput(c.ToString());
        ScanDetector.ProcessInput("\r");

        Assert.Equal(new[] { "ABC" }, _scans);
    }

}
