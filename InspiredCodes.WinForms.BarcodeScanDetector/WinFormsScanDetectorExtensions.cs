using System.Windows.Forms;
using InspiredCodes.WPF.BarcodeScanDetector;

namespace InspiredCodes.WinForms.BarcodeScanDetector;

/// <summary>
/// Feeds the keys typed into a control to the global <see cref="ScanDetector"/>. The detector only
/// observes: the key still reaches the focused control. All registered controls share one detector.
/// Registering the same control again is harmless, and a single UnRegister detaches it.
/// </summary>
public static class WinFormsScanDetectorExtensions
{
    /// <summary>
    /// A form only receives the KeyPress of its child controls if <see cref="Form.KeyPreview"/> is
    /// <c>true</c>; to catch scans wherever the focus is, set it on the form you register.
    /// </summary>
    public static void RegisterKeyPress(this Control control)
    {
        // detach first: registering twice must not forward every key twice
        control.KeyPress -= Control_KeyPress;
        control.KeyPress += Control_KeyPress;
    }

    public static void UnRegisterKeyPress(this Control control)
    {
        control.KeyPress -= Control_KeyPress;
    }

    private static void Control_KeyPress(object? sender, KeyPressEventArgs e)
    {
        ScanDetector.ProcessInput(e.KeyChar.ToString());
    }
}
