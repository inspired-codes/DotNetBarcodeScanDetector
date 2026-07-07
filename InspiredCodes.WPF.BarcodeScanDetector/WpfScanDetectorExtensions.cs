using System.Windows;
using System.Windows.Input;

namespace InspiredCodes.WPF.BarcodeScanDetector;

public static class WpfScanDetectorExtensions
{
    public static void RegisterPreviewTextInput(this IInputElement inputElement)
    {
        inputElement.PreviewTextInput += OnPreviewTextInput;
    }

    public static void RegisterTextInput(this IInputElement inputElement)
    {
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
