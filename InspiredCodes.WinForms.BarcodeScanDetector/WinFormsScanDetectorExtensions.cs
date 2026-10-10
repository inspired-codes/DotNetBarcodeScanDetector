using System.Runtime.CompilerServices;
using System.Windows.Forms;
using InspiredCodes.BarcodeScanDetector;

namespace InspiredCodes.WinForms.BarcodeScanDetector;

/// <summary>
/// Feeds the keys typed into a control to a <see cref="ScanDetectorEngine"/>, by default
/// <see cref="ScanDetector.Default"/>. The engine only observes: the key still reaches the focused
/// control. To catch keys wherever the focus is without <see cref="Form.KeyPreview"/>, use
/// <see cref="BarcodeScanMessageFilter"/> instead (not both for the same engine: it would see every key twice).
/// </summary>
/// <remarks>
/// Registering the same control with the same engine again is harmless, and one UnRegister call
/// detaches it. The engine is fed through <see cref="ScanDetectorEngine.ProcessInput(string)"/>,
/// i.e. with its own clock, so don't also feed it caller timestamps.
/// </remarks>
public static class WinFormsScanDetectorExtensions
{
    /// <summary>the handler registered per control and engine</summary>
    private static readonly ConditionalWeakTable<Control, Dictionary<ScanDetectorEngine, KeyPressEventHandler>> s_handlers =
        new ConditionalWeakTable<Control, Dictionary<ScanDetectorEngine, KeyPressEventHandler>>();

    /// <summary>
    /// A form only receives the KeyPress of its child controls if <see cref="Form.KeyPreview"/> is
    /// <c>true</c>; to catch scans wherever the focus is, set it on the form you register.
    /// </summary>
    public static void RegisterKeyPress(this Control control, ScanDetectorEngine? engine = null)
    {
        if (control == null)
            throw new ArgumentNullException(nameof(control));
        engine ??= ScanDetector.Default;

        var handlers = s_handlers.GetValue(control, _ => new Dictionary<ScanDetectorEngine, KeyPressEventHandler>());
        lock (handlers)
        {
            if (handlers.ContainsKey(engine))
                return;

            KeyPressEventHandler handler = (sender, e) => engine.ProcessInput(e.KeyChar.ToString());
            control.KeyPress += handler;
            handlers.Add(engine, handler);
        }
    }

    public static void UnRegisterKeyPress(this Control control, ScanDetectorEngine? engine = null)
    {
        if (control == null)
            throw new ArgumentNullException(nameof(control));
        engine ??= ScanDetector.Default;

        if (!s_handlers.TryGetValue(control, out var handlers))
            return;
        lock (handlers)
        {
            if (!handlers.TryGetValue(engine, out var handler))
                return;

            control.KeyPress -= handler;
            handlers.Remove(engine);
        }
    }
}
