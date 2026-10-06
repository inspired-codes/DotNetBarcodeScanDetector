using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using InspiredCodes.WPF.BarcodeScanDetector;

namespace InspiredCodes.BarcodeScanDetector.Tests;

/// <summary>
/// Input may arrive from several threads (UI thread, a serial port reader, tests...).
/// These tests only assert invariants that must hold for every interleaving.
/// </summary>
[TestClass()]
public class ConcurrencyTests
{

    private long _savedThresholdTicks;
    private ConcurrentQueue<string> _scans;
    private EventHandler<BarcodeScannedEventArgs> _handler;

    [TestInitialize]
    public void InitializeTest()
    {
        _savedThresholdTicks = DetectorConfig.ThresholdTicks;
        // timing must not decide these tests: a stalled thread must not turn fast input into slow input
        DetectorConfig.ThresholdMillisec = 10000;

        ScanDetector.Reset();
        _scans = new ConcurrentQueue<string>();
        _handler = (sender, e) => _scans.Enqueue(e.InputText);
        ScanDetector.BarcodeScanned += _handler;
    }

    [TestCleanup]
    public void CleanupTest()
    {
        ScanDetector.BarcodeScanned -= _handler;
        DetectorConfig.ThresholdTicks = _savedThresholdTicks;
        ScanDetector.Reset();
    }

    [TestMethod]
    public void SimultaneousNewlines_AfterABufferedScan_RaiseExactlyOneEvent()
    {
        const int Workers = 4;
        const int Rounds = 1500;

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
                    ScanDetector.ProcessInput("\r");
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
            ScanDetector.Reset();
            _scans = new ConcurrentQueue<string>();
            foreach (char c in "ABC")
                ScanDetector.ProcessInput(c.ToString());

            start.SignalAndWait();   // all workers send "\r" at the same time
            done.SignalAndWait();

            string[] scans = _scans.ToArray();
            if (scans.Length != 1 || scans[0] != "ABC")
                failures.Enqueue($"round {round}: expected exactly [ABC], got [{string.Join(", ", scans)}]");
        }

        threads.ForEach(t => t.Join());
        Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures.Take(5)));
    }

    [TestMethod]
    public void HandlerRunsOutsideTheDetectorLock()
    {
        bool otherThreadFinished = false;
        EventHandler<BarcodeScannedEventArgs> feedingHandler = (sender, e) =>
        {
            // another thread feeds input while this handler is still running
            var other = new Thread(() => ScanDetector.ProcessInput("Z")) { IsBackground = true };
            other.Start();
            otherThreadFinished = other.Join(TimeSpan.FromSeconds(5));
        };

        // the detector is static: a handler left subscribed would feed input into every later test
        ScanDetector.BarcodeScanned += feedingHandler;
        try
        {
            foreach (char c in "ABC")
                ScanDetector.ProcessInput(c.ToString());
            ScanDetector.ProcessInput("\r");
        }
        finally
        {
            ScanDetector.BarcodeScanned -= feedingHandler;
        }

        Assert.IsTrue(otherThreadFinished,
            "another thread's input was blocked while the handler ran: the event is raised inside the detector lock");
    }

    [TestMethod]
    public void ConcurrentInputAndReset_DoNotThrow()
    {
        var failures = new ConcurrentQueue<string>();
        using var stop = new ManualResetEventSlim(false);

        void Run(string name, Action body) => new Thread(() =>
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
        { IsBackground = true }.Start();

        int[] counters = new int[1];
        string chars = "ABC123\r\n";
        for (int worker = 0; worker < 3; worker++)
        {
            int seed = worker;
            Run("input", () => ScanDetector.ProcessInput(chars[(Interlocked.Increment(ref counters[0]) + seed) % chars.Length].ToString()));
        }
        Run("reset", () => { ScanDetector.Reset(); Thread.Sleep(1); });

        Thread.Sleep(400);
        stop.Set();
        Thread.Sleep(50);

        Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures.Take(5)));
    }

}
