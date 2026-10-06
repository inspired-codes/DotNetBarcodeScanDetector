using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using Xunit;

using InspiredCodes.WPF.BarcodeScanDetector;

namespace InspiredCodes.BarcodeScanDetector.Tests;

/// <summary>
/// Cooldown behaviour around the newline that ends a scan.
///
/// These tests use real time (the detector has no injectable clock yet), so they
/// widen the fast-input threshold to 150ms and measure everything from the moment
/// the scan completed. The detector's 300ms cooldown then ends at +300ms, or at
/// +400ms if a fast input at +100ms extended it; probing at +350ms tells the two
/// apart with ~50ms of slack on either side.
/// </summary>
public class CooldownNewlineTests : IDisposable
{

    private const int ThresholdMs = 150;
    private const int ProbeAtMs = 350;
    private const int SlackMs = 30;

    private long _savedThresholdTicks;
    private List<string> _scans;
    private Stopwatch _sinceScan;
    private Action _afterScan;
    private EventHandler<BarcodeScannedEventArgs> _handler;

    public CooldownNewlineTests()
    {
        _savedThresholdTicks = DetectorConfig.ThresholdTicks;
        DetectorConfig.ThresholdTicks = ThresholdMs * TimeSpan.TicksPerMillisecond;

        ScanDetector.Reset();
        _scans = new List<string>();
        _afterScan = null;
        _handler = (sender, e) =>
        {
            _scans.Add(e.InputText);
            _afterScan?.Invoke();
        };
        ScanDetector.BarcodeScanned += _handler;
    }

    public void Dispose()
    {
        ScanDetector.BarcodeScanned -= _handler;
        DetectorConfig.ThresholdTicks = _savedThresholdTicks;
        ScanDetector.Reset();
    }

    [Theory]
    [InlineData("\r", "\n")]
    [InlineData("\n", "\r")]
    public void ComplementOfEndingNewline_DoesNotExtendCooldown(string ending, string complement)
    {
        CompleteScan("ABC", ending);
        SendAt(100, complement);

        ProbeScan();

        Assert.Equal(new[] { "ABC", "XYZ" }, _scans);
    }

    [Theory]
    [InlineData("\r")]
    [InlineData("\n")]
    public void SameNewlineAgain_StillExtendsCooldown(string ending)
    {
        CompleteScan("ABC", ending);
        SendAt(100, ending);

        ProbeScan();

        Assert.Equal(new[] { "ABC" }, _scans);
    }

    [Fact]
    public void OrdinaryFastInput_StillExtendsCooldown()
    {
        CompleteScan("ABC", "\r");
        SendAt(100, "Q");

        ProbeScan();

        Assert.Equal(new[] { "ABC" }, _scans);
    }

    [Fact]
    public void OnlyTheFirstComplement_IsSwallowed()
    {
        CompleteScan("ABC", "\r");
        SendAt(50, "\n");   // swallowed, cooldown stays at +300ms
        SendAt(100, "\n");  // a second one is ordinary fast input: extends to +400ms

        ProbeScan();

        Assert.Equal(new[] { "ABC" }, _scans);
    }

    [Fact]
    public void ComplementAfterOrdinaryInput_IsNotSwallowed()
    {
        CompleteScan("ABC", "\r");
        SendAt(10, "Q");    // extends to +310ms and uses up the "next input" slot
        SendAt(100, "\n");  // no longer the immediate complement: extends to +400ms

        ProbeScan();

        Assert.Equal(new[] { "ABC" }, _scans);
    }

    [Fact]
    public void InputFedFromHandler_SeesActiveCooldown()
    {
        bool fed = false;
        _afterScan = () =>
        {
            if (fed)
                return;
            fed = true;
            Feed("Z");
            ScanDetector.ProcessInput("\r");
        };

        Feed("ABC");
        ScanDetector.ProcessInput("\r");

        // the cooldown is already running while the handler executes, so the
        // handler's own fast input is discarded instead of producing a second scan
        Assert.Equal(new[] { "ABC" }, _scans);
    }

    private void CompleteScan(string text, string ending)
    {
        Feed(text);
        ScanDetector.ProcessInput(ending);
        _sinceScan = Stopwatch.StartNew();
        Assert.True(_scans.Count == 1, "the scan should have completed immediately");
    }

    private void SendAt(int ms, string input)
    {
        WaitUntil(ms);
        if (_sinceScan.ElapsedMilliseconds > ms + SlackMs)
            Assert.Fail("test thread was delayed too long for the timing to be meaningful");
        ScanDetector.ProcessInput(input);
    }

    /// <summary>
    /// Sends a second scan at +350ms: accepted if the cooldown ended at +300ms,
    /// discarded if it was extended to +400ms.
    /// </summary>
    private void ProbeScan()
    {
        WaitUntil(ProbeAtMs);
        if (_sinceScan.ElapsedMilliseconds > ProbeAtMs + SlackMs)
            Assert.Fail("test thread was delayed too long for the timing to be meaningful");
        Feed("XYZ");
        ScanDetector.ProcessInput("\r");
    }

    private void WaitUntil(int ms)
    {
        while (_sinceScan.ElapsedMilliseconds < ms)
            Thread.Sleep(1);
    }

    private static void Feed(string text)
    {
        foreach (char c in text)
            ScanDetector.ProcessInput(c.ToString());
    }

}
