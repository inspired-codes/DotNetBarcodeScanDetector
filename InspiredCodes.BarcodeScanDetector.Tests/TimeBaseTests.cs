using System;
using System.Collections.Generic;

using Xunit;

namespace InspiredCodes.BarcodeScanDetector.Tests;

/// <summary>
/// OD-3: an engine uses either its own clock (ProcessInput(text), Simulate) or the caller's
/// timestamps (ProcessInput(text, timestamp), ProcessBatch), never both until Reset.
/// </summary>
public class TimeBaseTests
{

    [Fact]
    public void CallerTimestamps_AfterTheOwnClock_Throw()
    {
        var engine = new ScanDetectorEngine();
        engine.ProcessInput("A");

        Assert.Throws<InvalidOperationException>(() => engine.ProcessInput("B", TimeSpan.Zero));
        Assert.Throws<InvalidOperationException>(() => engine.ProcessBatch(new[] { new KeyInput("B", TimeSpan.Zero) }));
    }

    [Fact]
    public void OwnClock_AfterCallerTimestamps_Throws()
    {
        var engine = new ScanDetectorEngine();
        engine.ProcessInput("A", TimeSpan.Zero);

        Assert.Throws<InvalidOperationException>(() => engine.ProcessInput("B"));
        Assert.Throws<InvalidOperationException>(() => engine.Simulate("B"));
    }

    [Fact]
    public void RejectedInput_ChangesNothing()
    {
        var r = new Recorder();
        r.Type("AB", 0);

        Assert.Throws<InvalidOperationException>(() => r.Engine.ProcessInput("C"));
        r.Key("\r", 2);

        Assert.Equal(new[] { "AB" }, r.Scans);
    }

    [Fact]
    public void Reset_AllowsTheOtherTimeBase()
    {
        var engine = new ScanDetectorEngine();
        engine.ProcessInput("A");

        engine.Reset();
        engine.ProcessInput("B", TimeSpan.Zero);

        engine.Reset();
        engine.ProcessInput("C");
    }

    [Fact]
    public void EarlierTimestamp_CountsAsNoTimePassed()
    {
        var r = new Recorder();

        r.Key("A", 100);
        r.Key("B", 50);           // earlier than A: fast
        r.Key("\r", 51);

        Assert.Equal(new[] { "AB" }, r.Scans);
    }

    [Fact]
    public void OwnClock_DetectsInputTypedBackToBack()
    {
        var engine = new ScanDetectorEngine();
        var scans = new List<string>();
        engine.BarcodeScanned += (sender, e) => scans.Add(e.InputText);

        foreach (char c in "ABC\r")
            engine.ProcessInput(c.ToString());

        Assert.Equal(new[] { "ABC" }, scans);
    }

    [Fact]
    public void OwnClock_TimestampsDoNotGoBackwards()
    {
        var engine = new ScanDetectorEngine(new ScanDetectorOptions { Cooldown = TimeSpan.Zero });
        var stamps = new List<TimeSpan>();
        engine.BarcodeScanned += (sender, e) => stamps.Add(e.Timestamp);

        engine.Simulate("ONE");
        engine.Simulate("TWO");

        Assert.Equal(2, stamps.Count);
        Assert.True(stamps[0] >= TimeSpan.Zero);
        Assert.True(stamps[1] >= stamps[0]);
    }

}
