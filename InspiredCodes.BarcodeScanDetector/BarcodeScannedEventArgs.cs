using System;

namespace InspiredCodes.BarcodeScanDetector;

public sealed class BarcodeScannedEventArgs : EventArgs
{

    public BarcodeScannedEventArgs(string inputText, TimeSpan timestamp)
    {
        InputText = inputText ?? throw new ArgumentNullException(nameof(inputText));
        Timestamp = timestamp;
    }

    /// <summary>the scanned text, without the terminator</summary>
    public string InputText { get; }

    /// <summary>
    /// when the terminator arrived, in the engine's time base: its own monotonic clock, or the
    /// timestamps the caller supplied
    /// </summary>
    public TimeSpan Timestamp { get; }

}
