using System;
using System.Collections.Generic;

using Xunit;

namespace InspiredCodes.BarcodeScanDetector.Tests;

/// <summary>
/// ProcessBatch: inputs with the caller's timestamps, processed as one step.
/// </summary>
public class BatchTests
{

    private static KeyInput[] Keys(string keys, double atMs, double stepMs = 1)
    {
        var inputs = new KeyInput[keys.Length];
        for (int i = 0; i < keys.Length; i++)
            inputs[i] = new KeyInput(keys[i].ToString(), Recorder.Ms(atMs + i * stepMs));
        return inputs;
    }

    [Fact]
    public void SeveralScansInOneBatch_AreRaisedInOrder()
    {
        var r = new Recorder();
        var batch = new List<KeyInput>();
        batch.AddRange(Keys("ONE\r", 0));
        batch.AddRange(Keys("TWO\r", 400));     // after the first scan's cooldown

        r.Engine.ProcessBatch(batch);

        Assert.Equal(new[] { "ONE", "TWO" }, r.Scans);
    }

    [Fact]
    public void BatchesAndSingleInputs_ShareOneTimeline()
    {
        var r = new Recorder();

        r.Engine.ProcessBatch(Keys("AB", 0));
        r.Key("C", 2);
        r.Engine.ProcessBatch(Keys("\r", 3));

        Assert.Equal(new[] { "ABC" }, r.Scans);
    }

    [Fact]
    public void EmptyBatch_DoesNothing_AndDoesNotFixTheTimeBase()
    {
        var engine = new ScanDetectorEngine();

        engine.ProcessBatch(new KeyInput[0]);

        engine.ProcessInput("A");   // own clock still allowed
    }

    [Fact]
    public void NullBatch_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new ScanDetectorEngine().ProcessBatch(null));
    }

    [Fact]
    public void InputWithoutText_RejectsTheWholeBatch()
    {
        var r = new Recorder();

        Assert.Throws<ArgumentException>(() => r.Engine.ProcessBatch(new[] { new KeyInput("A", Recorder.Ms(0)), default(KeyInput) }));
        r.Engine.ProcessBatch(Keys("BC\r", 2));

        // had "A" been processed, B at 2 ms would have been fast after it, giving "ABC"
        Assert.Equal(new[] { "BC" }, r.Scans);
    }

}
