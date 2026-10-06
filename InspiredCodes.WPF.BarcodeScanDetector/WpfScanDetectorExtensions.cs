using System.Windows;
using System.Windows.Input;
using InspiredCodes.BarcodeScanDetector;

namespace InspiredCodes.WPF.BarcodeScanDetector;

/// <summary>
/// Feeds the text typed into an element to the global <see cref="ScanDetector"/>. The detector only
/// observes: input still reaches the focused control. All registered elements share one detector.
/// Registering the same element again is harmless, and a single UnRegister detaches it.
/// </summary>
public static class WpfScanDetectorExtensions
{
    /// <summary>tunnelling (preview) event: raises <see cref="ScanDetector.PreviewBarcodeScanned"/></summary>
    public static void RegisterPreviewTextInput(this IInputElement inputElement)
    {
        // detach first: registering twice must not forward every input twice
        inputElement.PreviewTextInput -= OnPreviewTextInput;
        inputElement.PreviewTextInput += OnPreviewTextInput;
    }

    /// <summary>bubbling event: raises <see cref="ScanDetector.BarcodeScanned"/></summary>
    public static void RegisterTextInput(this IInputElement inputElement)
    {
        // detach first: registering twice must not forward every input twice
        inputElement.TextInput -= OnTextInput;
        inputElement.TextInput += OnTextInput;
    }

    public static void UnRegisterPreviewTextInput(this IInputElement inputElement)
    {
        inputElement.PreviewTextInput -= OnPreviewTextInput;
    }

    public static void UnRegisterTextInput(this IInputElement inputElement)
    {
        inputElement.TextInput -= OnTextInput;
    }

    private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        ScanDetector.ProcessPreviewInput(e.Text);
    }

    private static void OnTextInput(object sender, TextCompositionEventArgs e)
    {
        ScanDetector.ProcessInput(e.Text);
    }
}
