using Xunit;

namespace InspiredCodes.BarcodeScanDetector.Tests;

/// <summary>
/// R1: what ends a scan (Enter in any form, Tab when selected); R4: a terminator with nothing
/// buffered is not a scan.
/// </summary>
public class TerminatorTests
{

    [Theory]
    [InlineData("\r")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("\n\r")]
    public void Enter_InAnyForm_CompletesAScan(string terminator)
    {
        var r = new Recorder();

        r.Type("ABC", 0);
        r.Key(terminator, 3);

        Assert.Equal(new[] { "ABC" }, r.Scans);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("A\r")]
    [InlineData("\r\r")]
    [InlineData("\r\n\r\n")]
    public void OtherInput_IsOrdinaryText(string text)
    {
        var r = new Recorder();

        r.Type("AB", 0);
        r.Key(text, 2);
        Assert.Empty(r.Scans);

        r.Key("\r", 3);
        Assert.Equal(new[] { "AB" + text }, r.Scans);
    }

    [Fact]
    public void Tab_CompletesAScan_WhenSelected()
    {
        var r = new Recorder(new ScanDetectorOptions { Terminators = ScanTerminators.Tab });

        r.Type("ABC", 0);
        r.Key("\t", 3);

        Assert.Equal(new[] { "ABC" }, r.Scans);
    }

    [Fact]
    public void Enter_IsOrdinaryText_WhenOnlyTabIsSelected()
    {
        var r = new Recorder(new ScanDetectorOptions { Terminators = ScanTerminators.Tab });

        r.Type("AB", 0);
        r.Key("\r", 2);
        r.Key("\t", 3);

        Assert.Equal(new[] { "AB\r" }, r.Scans);
    }

    [Theory]
    [InlineData("\r")]
    [InlineData("\t")]
    public void EnterAndTab_BothCompleteAScan_WhenBothAreSelected(string terminator)
    {
        var r = new Recorder(new ScanDetectorOptions { Terminators = ScanTerminators.Enter | ScanTerminators.Tab });

        r.Type("ABC", 0);
        r.Key(terminator, 3);

        Assert.Equal(new[] { "ABC" }, r.Scans);
    }

    [Fact]
    public void SecondTab_AfterATabTerminatedScan_ExtendsTheCooldown()
    {
        // only CR and LF form pairs (R2); a second Tab is ordinary fast input during the cooldown
        var r = new Recorder(new ScanDetectorOptions { Terminators = ScanTerminators.Tab });
        r.Type("ABC", 0);
        r.Key("\t", 3);           // cooldown until 303

        r.Key("\t", 23);          // extends it to 323
        r.Probe(310, "\t");

        Assert.Equal(new[] { "ABC" }, r.Scans);
    }

    [Fact]
    public void SecondTab_ProbeAfterTheExtendedCooldown_IsAccepted()
    {
        // the counterpart of the test above, so it can't pass just because the probe never completes
        var r = new Recorder(new ScanDetectorOptions { Terminators = ScanTerminators.Tab });
        r.Type("ABC", 0);
        r.Key("\t", 3);
        r.Key("\t", 23);

        r.Probe(323, "\t");

        Assert.Equal(new[] { "ABC", "XYZ" }, r.Scans);
    }

    [Theory]
    [InlineData(ScanTerminators.Enter, "\r")]
    [InlineData(ScanTerminators.Tab, "\t")]
    public void FastTerminator_WithNothingBuffered_RaisesNothingAndStartsNoCooldown(ScanTerminators terminators, string terminator)
    {
        var r = new Recorder(new ScanDetectorOptions { Terminators = terminators });
        r.Type("ABC", 0);
        r.Key(terminator, 3);     // a scan; cooldown until 303
        r.Key("Q", 290);          // discarded by the cooldown, nothing buffered afterwards

        r.Key(terminator, 305);   // fast, after the cooldown, with nothing before it: not a scan

        // and it started no cooldown that would swallow this scan
        r.Type("XYZ", 306);
        r.Key(terminator, 309);
        Assert.Equal(new[] { "ABC", "XYZ" }, r.Scans);
    }

}
