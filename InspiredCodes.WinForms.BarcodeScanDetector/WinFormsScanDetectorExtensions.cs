using System.Windows.Forms;
using InspiredCodes.WPF.BarcodeScanDetector;

namespace InspiredCodes.WinForms.BarcodeScanDetector;

public static class WinFormsScanDetectorExtensions
{
    public static void RegisterKeyPress(this Control control)
    {
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
