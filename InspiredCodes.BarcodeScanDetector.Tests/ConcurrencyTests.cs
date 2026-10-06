using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

using Xunit;

namespace InspiredCodes.BarcodeScanDetector.Tests;

/// <summary>
/// R8: input may arrive from several threads (UI thread, a serial port reader, tests...).
/// These tests only assert invariants that must hold for every interleaving.
/// </summary>
public class ConcurrencyTests
{

    // timing must not decide these tests: a stalled thread must not turn fast input into slow input
    private static ScanDetectorEngine CreateEngine() =>
        new ScanDetectorEngine(new ScanDetectorOptions { InterKeyThreshold = TimeSpan.FromSeconds(10) });

    [Fact]
    public void SimultaneousTerminators_AfterABufferedScan_RaiseExactlyOneEvent()
    {
        const int Workers = 4;
        const int Rounds = 1500;

        var engine = CreateEngine();
        var scans = new ConcurrentQueue<string>();
        engine.BarcodeScanned += (sender, e) => scans.Enqueue(e.InputText);
        var failures = new ConcurrentQueue<string>();
        using var start = new Barrier(Workers + 1);
        using var done = new Barrier(Workers + 1);

        var threads = Enumerable.Range(0, Workers).Select(_ => new Thread(() =>
        {
            for (int round = 0; round < Rounds; round++)
            {
                start.SignalAndWait();
                try
                {
                    engine.ProcessInput("\r");
                }
                catch (Exception ex)
                {
                    failures.Enqueue($"round {round}: {ex.GetType().Name}: {ex.Message}");
                }
                done.SignalAndWait();
            }
        })
        { IsBackground = true }).ToList();
        threads.ForEach(t => t.Start());

        for (int round = 0; round < Rounds; round++)
        {
            engine.Reset();
            while (scans.TryDequeue(out _)) { }
            foreach (char c in "ABC")
                engine.ProcessInput(c.ToString());

            start.SignalAndWait();   // all workers send "\r" at the same time
            done.SignalAndWait();

            string[] result = scans.ToArray();
            if (result.Length != 1 || result[0] != "ABC")
                failures.Enqueue($"round {round}: expected exactly [ABC], got [{string.Join(", ", result)}]");
        }

        threads.ForEach(t => t.Join());
        Assert.True(failures.IsEmpty, string.Join(Environment.NewLine, failures.Take(5)));
    }

    [Fact]
    public void Handler_RunsOutsideTheEngineLock()
    {
        var engine = CreateEngine();
        bool otherThreadFinished = false;
        engine.BarcodeScanned += (sender, e) =>
        {
            // another thread feeds input while this handler is still running
            var other = new Thread(() => engine.ProcessInput("Z")) { IsBackground = true };
            other.Start();
            otherThreadFinished = other.Join(TimeSpan.FromSeconds(5));
        };

        foreach (char c in "ABC\r")
            engine.ProcessInput(c.ToString());

        Assert.True(otherThreadFinished,
            "another thread's input was blocked while the handler ran: the event is raised inside the engine lock");
    }

    [Fact]
    public void BatchHandler_RunsOutsideTheEngineLock()
    {
        var engine = CreateEngine();
        bool otherThreadFinished = false;
        engine.BarcodeScanned += (sender, e) =>
        {
            var other = new Thread(() => engine.ProcessInput("Z", Recorder.Ms(10))) { IsBackground = true };
            other.Start();
            otherThreadFinished = other.Join(TimeSpan.FromSeconds(5));
        };

        engine.ProcessBatch("ABC\r".Select((c, i) => new KeyInput(c.ToString(), Recorder.Ms(i))).ToList());

        Assert.True(otherThreadFinished,
            "another thread's input was blocked while the handler ran: batch events are raised inside the engine lock");
    }

    [Fact]
    public void ConcurrentInputAndReset_DoNotThrow()
    {
        var engine = CreateEngine();
        var failures = new ConcurrentQueue<string>();
        using var stop = new ManualResetEventSlim(false);

        Thread Run(string name, Action body)
        {
            var thread = new Thread(() =>
            {
                try
                {
                    while (!stop.IsSet)
                        body();
                }
                catch (Exception ex)
                {
                    failures.Enqueue($"{name}: {ex.GetType().Name}: {ex.Message}");
                }
            })
            { IsBackground = true };
            thread.Start();
            return thread;
        }

        int[] counter = new int[1];
        string chars = "ABC123\r\n";
        var threads = Enumerable.Range(0, 3)
            .Select(worker => Run("input", () => engine.ProcessInput(chars[(Interlocked.Increment(ref counter[0]) + worker) % chars.Length].ToString())))
            .ToList();
        threads.Add(Run("reset", () => { engine.Reset(); Thread.Sleep(1); }));

        Thread.Sleep(400);
        stop.Set();
        threads.ForEach(t => t.Join());

        Assert.True(failures.IsEmpty, string.Join(Environment.NewLine, failures.Take(5)));
    }

}
