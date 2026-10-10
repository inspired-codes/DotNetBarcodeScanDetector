using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using InspiredCodes.BarcodeScanDetector;

namespace InspiredCodes.WPF.BarcodeScanDetector;

/// <summary>
/// Feeds the text typed into an element (and its children) to a <see cref="ScanDetectorEngine"/>,
/// by default one of the process-wide engines of <see cref="ScanDetector"/>. The engine only
/// observes: input still reaches the focused control, and input that a control marks as handled
/// (a focused TextBox does) is seen too.
/// </summary>
/// <remarks>
/// Registering the same element with the same engine again is harmless, and one UnRegister call
/// detaches it. The engine is fed through <see cref="ScanDetectorEngine.ProcessInput(string)"/>,
/// i.e. with its own clock, so don't also feed it caller timestamps.
/// </remarks>
public static class WpfScanDetectorExtensions
{
    /// <summary>the handler registered per element, routed event and engine</summary>
    private static readonly ConditionalWeakTable<UIElement, Dictionary<(RoutedEvent Event, ScanDetectorEngine Engine), TextCompositionEventHandler>> s_handlers =
        new ConditionalWeakTable<UIElement, Dictionary<(RoutedEvent Event, ScanDetectorEngine Engine), TextCompositionEventHandler>>();

    /// <summary>
    /// tunnelling event (PreviewTextInput); the default engine is <see cref="ScanDetector.Preview"/>
    /// </summary>
    public static void RegisterPreviewTextInput(this UIElement element, ScanDetectorEngine? engine = null)
    {
        Register(element, TextCompositionManager.PreviewTextInputEvent, engine ?? ScanDetector.Preview);
    }

    /// <summary>
    /// bubbling event (TextInput); the default engine is <see cref="ScanDetector.Default"/>
    /// </summary>
    public static void RegisterTextInput(this UIElement element, ScanDetectorEngine? engine = null)
    {
        Register(element, TextCompositionManager.TextInputEvent, engine ?? ScanDetector.Default);
    }

    public static void UnRegisterPreviewTextInput(this UIElement element, ScanDetectorEngine? engine = null)
    {
        UnRegister(element, TextCompositionManager.PreviewTextInputEvent, engine ?? ScanDetector.Preview);
    }

    public static void UnRegisterTextInput(this UIElement element, ScanDetectorEngine? engine = null)
    {
        UnRegister(element, TextCompositionManager.TextInputEvent, engine ?? ScanDetector.Default);
    }

    private static void Register(UIElement element, RoutedEvent routedEvent, ScanDetectorEngine engine)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        var handlers = s_handlers.GetValue(element, _ => new Dictionary<(RoutedEvent, ScanDetectorEngine), TextCompositionEventHandler>());
        lock (handlers)
        {
            if (handlers.ContainsKey((routedEvent, engine)))
                return;

            TextCompositionEventHandler handler = (sender, e) => engine.ProcessInput(e.Text);
            // handledEventsToo: also see input that the focused control marks as handled
            element.AddHandler(routedEvent, handler, handledEventsToo: true);
            handlers.Add((routedEvent, engine), handler);
        }
    }

    private static void UnRegister(UIElement element, RoutedEvent routedEvent, ScanDetectorEngine engine)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        if (!s_handlers.TryGetValue(element, out var handlers))
            return;
        lock (handlers)
        {
            if (!handlers.TryGetValue((routedEvent, engine), out var handler))
                return;

            element.RemoveHandler(routedEvent, handler);
            handlers.Remove((routedEvent, engine));
        }
    }
}
