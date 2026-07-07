using System;

namespace InspiredCodes.WPF.BarcodeScanDetector;

public class BarcodeScannedEventArgs : EventArgs
{

    public BarcodeScannedEventArgs(string inputText)
    {
        InputText = inputText;
    }

    public string InputText { get; }
    public long TimestampTicks { get; } = DateTime.Now.Ticks;

}
