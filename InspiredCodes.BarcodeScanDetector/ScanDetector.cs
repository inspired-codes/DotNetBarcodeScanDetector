using System;

namespace InspiredCodes.BarcodeScanDetector;

/// <summary>
/// Convenience facade for single-window apps: two process-wide engines with default options,
/// <see cref="Default"/> for the bubbling input path (TextInput, KeyPress) and <see cref="Preview"/>
/// for the tunnelling one (PreviewTextInput). For other options, or one engine per window or
/// device, create a <see cref="ScanDetectorEngine"/>.
/// </summary>
public static class ScanDetector
{

    public static ScanDetectorEngine Default { get; } = new ScanDetectorEngine();
    public static ScanDetectorEngine Preview { get; } = new ScanDetectorEngine();

    /// <summary>raised by <see cref="Default"/>; the sender is that engine</summary>
    public static event EventHandler<BarcodeScannedEventArgs> BarcodeScanned
    {
        add { Default.BarcodeScanned += value; }
        remove { Default.BarcodeScanned -= value; }
    }
    /// <summary>raised by <see cref="Preview"/>; the sender is that engine</summary>
    public static event EventHandler<BarcodeScannedEventArgs> PreviewBarcodeScanned
    {
        add { Preview.BarcodeScanned += value; }
        remove { Preview.BarcodeScanned -= value; }
    }
    public static void ProcessInput(string text)
    {
        Default.ProcessInput(text);
    }
    public static void ProcessPreviewInput(string text)
    {
        Preview.ProcessInput(text);
    }
    public static void SimulateBubbleFastInput(string barcode)
    {
        Default.Simulate(barcode);
    }
    public static void SimulateTunnelFastInput(string barcode)
    {
        Preview.Simulate(barcode);
    }
    public static void Reset()
    {
        Preview.Reset();
        Default.Reset();
    }
}
