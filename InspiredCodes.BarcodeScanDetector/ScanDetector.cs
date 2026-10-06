namespace InspiredCodes.WPF.BarcodeScanDetector;

public class ScanDetector
{

    private static readonly GenericScanDetector PreviewTextInputDetector = new GenericScanDetector();
    private static readonly GenericScanDetector TextInputDetector = new GenericScanDetector();


    public static event EventHandler<BarcodeScannedEventArgs> BarcodeScanned
    {
        add { TextInputDetector.BarcodeScanned += value; }
        remove { TextInputDetector.BarcodeScanned -= value; }
    }
    public static event EventHandler<BarcodeScannedEventArgs> PreviewBarcodeScanned
    {
        add { PreviewTextInputDetector.BarcodeScanned += value; }
        remove { PreviewTextInputDetector.BarcodeScanned -= value; }
    }
    public static void ProcessPreviewInput(string text)
    {
        PreviewTextInputDetector.ProcessInput(text);
    }
    public static void ProcessInput(string text)
    {
        TextInputDetector.ProcessInput(text);
    }
    public static void SimulateBubbleFastInput(string barcode)
    {
        TextInputDetector.SimulateFastInput(null, barcode);
    }
    /// <param name="sender">reported as the sender of the <see cref="BarcodeScanned"/> event this simulation raises (real input reports null)</param>
    public static void SimulateBubbleFastInput(object sender, string barcode)
    {
        TextInputDetector.SimulateFastInput(sender, barcode);
    }
    public static void SimulateTunnelFastInput(string barcode)
    {
        PreviewTextInputDetector.SimulateFastInput(null, barcode);
    }
    /// <param name="sender">reported as the sender of the <see cref="PreviewBarcodeScanned"/> event this simulation raises (real input reports null)</param>
    public static void SimulateTunnelFastInput(object sender, string barcode)
    {
        PreviewTextInputDetector.SimulateFastInput(sender, barcode);
    }
    public static void Reset()
    {
        PreviewTextInputDetector.Reset();
        TextInputDetector.Reset();
    }
}
