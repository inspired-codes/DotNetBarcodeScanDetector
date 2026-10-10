using System.Windows.Forms;
using InspiredCodes.BarcodeScanDetector;

namespace InspiredCodes.WinForms.BarcodeScanDetector;

/// <summary>
/// Feeds every character typed into the application (each <c>WM_CHAR</c> message) to a
/// <see cref="ScanDetectorEngine"/>, by default <see cref="ScanDetector.Default"/>, whichever
/// control has the focus; no <see cref="Form.KeyPreview"/> needed. It only observes: every message
/// is passed on unchanged.
/// </summary>
/// <remarks>
/// Install it on the UI thread with <see cref="Application.AddMessageFilter"/> (and remove it with
/// <see cref="Application.RemoveMessageFilter"/>); it sees the messages of that thread's message loop.
/// Don't also register <see cref="WinFormsScanDetectorExtensions.RegisterKeyPress"/> with the same
/// engine: it would see every key twice.
/// </remarks>
public sealed class BarcodeScanMessageFilter : IMessageFilter
{
    private const int WM_CHAR = 0x0102;

    public BarcodeScanMessageFilter(ScanDetectorEngine? engine = null)
    {
        Engine = engine ?? ScanDetector.Default;
    }

    public ScanDetectorEngine Engine { get; }

    /// <returns>always <c>false</c>: the message is never swallowed</returns>
    public bool PreFilterMessage(ref Message m)
    {
        if (m.Msg == WM_CHAR)
            Engine.ProcessInput(unchecked((char)m.WParam.ToInt64()).ToString());
        return false;
    }
}
