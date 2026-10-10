using System;
using System.Collections.Generic;

namespace InspiredCodes.BarcodeScanDetector.Tests;

/// <summary>
/// An engine plus everything it reports. Feeds input with explicit timestamps in milliseconds
/// (the caller time base), so tests control time exactly and never sleep.
/// </summary>
internal sealed class Recorder
{
    public Recorder(ScanDetectorOptions options = null)
    {
        Engine = new ScanDetectorEngine(options);
        Engine.BarcodeScanned += (sender, e) =>
        {
            Senders.Add(sender);
            Events.Add(e);
        };
    }

    public ScanDetectorEngine Engine { get; }
    public List<object> Senders { get; } = new List<object>();
    public List<BarcodeScannedEventArgs> Events { get; } = new List<BarcodeScannedEventArgs>();
    public List<string> Scans => Events.ConvertAll(e => e.InputText);

    /// <summary>one input at <paramref name="atMs"/></summary>
    public void Key(string text, double atMs) => Engine.ProcessInput(text, Ms(atMs));

    /// <summary>
    /// each character of <paramref name="keys"/> as its own input, <paramref name="stepMs"/>
    /// apart, starting at <paramref name="atMs"/>
    /// </summary>
    /// <returns>the time of the last input</returns>
    public double Type(string keys, double atMs, double stepMs = 1)
    {
        for (int i = 0; i < keys.Length; i++)
            Key(keys[i].ToString(), atMs + i * stepMs);
        return atMs + (keys.Length - 1) * stepMs;
    }

    /// <summary>a second scan, "XYZ" plus <paramref name="terminator"/>, starting at <paramref name="atMs"/></summary>
    public void Probe(double atMs, string terminator = "\r")
    {
        Type("XYZ", atMs);
        Key(terminator, atMs + 3);
    }

    // exact on every target framework (net48's FromMilliseconds rounds to whole milliseconds)
    public static TimeSpan Ms(double ms) => TimeSpan.FromTicks((long)(ms * TimeSpan.TicksPerMillisecond));
}
