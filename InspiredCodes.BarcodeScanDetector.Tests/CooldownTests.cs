using Xunit;

namespace InspiredCodes.BarcodeScanDetector.Tests;

/// <summary>
/// R2 (CR/LF pairs), R3 (cooldown before the event) and R6 (cooldown length and extension).
/// Default options: 32 ms threshold, 300 ms cooldown. Each test scans "ABC" with the
/// terminator at 3 ms, so the cooldown runs until 303 ms unless something extends it.
/// </summary>
public class CooldownTests
{

    [Theory]
    [InlineData("\r", "\n")]
    [InlineData("\n", "\r")]
    public void ComplementOfTheEndingNewline_DoesNotExtendTheCooldown(string ending, string complement)
    {
        var r = new Recorder();
        r.Type("ABC", 0);
        r.Key(ending, 3);

        r.Key(complement, 23);    // fast, but the second half of the pair
        r.Probe(303);             // so the cooldown still ends at 303

        Assert.Equal(new[] { "ABC", "XYZ" }, r.Scans);
    }

    [Theory]
    [InlineData("\r")]
    [InlineData("\n")]
    public void SameNewlineAgain_ExtendsTheCooldown(string ending)
    {
        var r = new Recorder();
        r.Type("ABC", 0);
        r.Key(ending, 3);

        r.Key(ending, 23);        // not a complement: extends to 323
        r.Probe(310);

        Assert.Equal(new[] { "ABC" }, r.Scans);
    }

    [Fact]
    public void OrdinaryFastInput_ExtendsTheCooldown()
    {
        var r = new Recorder();
        r.Type("ABC", 0);
        r.Key("\r", 3);

        r.Key("Q", 23);           // extends to 323
        r.Probe(310);

        Assert.Equal(new[] { "ABC" }, r.Scans);
    }

    [Fact]
    public void OnlyTheFirstComplement_IsSwallowed()
    {
        var r = new Recorder();
        r.Type("ABC", 0);
        r.Key("\r", 3);

        r.Key("\n", 13);          // swallowed: the cooldown stays at 303
        r.Key("\n", 23);          // a second one is ordinary fast input: extends to 323
        r.Probe(310);

        Assert.Equal(new[] { "ABC" }, r.Scans);
    }

    [Fact]
    public void ComplementAfterOtherInput_IsNotSwallowed()
    {
        var r = new Recorder();
        r.Type("ABC", 0);
        r.Key("\r", 3);

        r.Key("Q", 13);           // extends to 313 and uses up the "next input" slot
        r.Key("\n", 23);          // no longer the immediate complement: extends to 323
        r.Probe(315);

        Assert.Equal(new[] { "ABC" }, r.Scans);
    }

    [Fact]
    public void InputFedFromTheHandler_RunsIntoTheCooldown()
    {
        var r = new Recorder();
        bool fed = false;
        r.Engine.BarcodeScanned += (sender, e) =>
        {
            if (fed)
                return;
            fed = true;
            r.Key("Z", 4);
            r.Key("\r", 5);
        };

        r.Type("ABC", 0);
        r.Key("\r", 3);

        // the cooldown is in place before the handler runs, so its input is no second scan
        Assert.Equal(new[] { "ABC" }, r.Scans);
    }

    [Fact]
    public void Cooldown_EndsExactlyCooldownAfterTheScan()
    {
        var r = new Recorder();
        r.Type("ABC", 0);
        r.Key("\r", 3);

        r.Probe(302);             // X at 302 is discarded; Y at 303 is the first accepted input

        Assert.Equal(new[] { "ABC", "YZ" }, r.Scans);
    }

    [Fact]
    public void SlowInputDuringTheCooldown_DoesNotExtendIt()
    {
        var r = new Recorder();
        r.Type("ABC", 0);
        r.Key("\r", 3);

        r.Key("Q", 103);          // 100 ms after the last input: slow
        r.Probe(303);

        Assert.Equal(new[] { "ABC", "XYZ" }, r.Scans);
    }

    [Theory]
    [InlineData(115, false)]      // before 23 + 100
    [InlineData(125, true)]       // after it
    public void FastInputDuringTheCooldown_ExtendsItByTheFullCooldown(double probeAt, bool accepted)
    {
        var r = new Recorder(new ScanDetectorOptions { Cooldown = Recorder.Ms(100) });
        r.Type("ABC", 0);
        r.Key("\r", 3);           // cooldown until 103

        r.Key("Q", 23);           // extends it to 123
        r.Probe(probeAt);

        Assert.Equal(accepted ? new[] { "ABC", "XYZ" } : new[] { "ABC" }, r.Scans);
    }

    [Fact]
    public void ZeroCooldown_AcceptsBackToBackScans()
    {
        var r = new Recorder(new ScanDetectorOptions { Cooldown = System.TimeSpan.Zero });

        r.Type("ONE", 0);
        r.Key("\r", 3);
        r.Type("TWO", 4);
        r.Key("\r", 7);

        Assert.Equal(new[] { "ONE", "TWO" }, r.Scans);
    }

}
