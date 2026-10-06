using System;

namespace InspiredCodes.BarcodeScanDetector;

/// <summary>
/// One input and the time it arrived, in the caller's time base (see <see cref="ScanDetectorEngine.ProcessBatch"/>).
/// </summary>
public readonly struct KeyInput
{
    public KeyInput(string text, TimeSpan timestamp)
    {
        Text = text;
        Timestamp = timestamp;
    }

    public string Text { get; }
    public TimeSpan Timestamp { get; }
}
