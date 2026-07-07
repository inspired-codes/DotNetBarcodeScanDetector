using System.Windows;
using System.Windows.Input;

namespace InspiredCodes.WPF.BarcodeScanDetector.Tests;

public class MockInputElement : IInputElement
{
    public event TextCompositionEventHandler TextInput;
    public event TextCompositionEventHandler PreviewTextInput;

    public void RaiseTextInput(TextCompositionEventArgs e) => TextInput?.Invoke(this, e);
    public void RaisePreviewTextInput(TextCompositionEventArgs e) => PreviewTextInput?.Invoke(this, e);

    // Required interface members (not used in tests)
    public bool IsMouseOver => false;
    public bool IsMouseDirectlyOver => false;
    public bool IsMouseCaptured => false;
    public bool IsStylusOver => false;
    public bool IsStylusDirectlyOver => false;
    public bool IsStylusCaptured => false;
    public bool IsKeyboardFocusWithin => false;
    public bool IsKeyboardFocused => false;
    public bool IsEnabled => true;
    public bool Focusable { get; set; }

    public event MouseButtonEventHandler PreviewMouseLeftButtonDown { add {} remove {} }
    public event MouseButtonEventHandler MouseLeftButtonDown { add {} remove {} }
    public event MouseButtonEventHandler PreviewMouseLeftButtonUp { add {} remove {} }
    public event MouseButtonEventHandler MouseLeftButtonUp { add {} remove {} }
    public event MouseButtonEventHandler PreviewMouseRightButtonDown { add {} remove {} }
    public event MouseButtonEventHandler MouseRightButtonDown { add {} remove {} }
    public event MouseButtonEventHandler PreviewMouseRightButtonUp { add {} remove {} }
    public event MouseButtonEventHandler MouseRightButtonUp { add {} remove {} }
    public event MouseEventHandler PreviewMouseMove { add {} remove {} }
    public event MouseEventHandler MouseMove { add {} remove {} }
    public event MouseWheelEventHandler PreviewMouseWheel { add {} remove {} }
    public event MouseWheelEventHandler MouseWheel { add {} remove {} }
    public event MouseEventHandler MouseEnter { add {} remove {} }
    public event MouseEventHandler MouseLeave { add {} remove {} }
    public event MouseEventHandler GotMouseCapture { add {} remove {} }
    public event MouseEventHandler LostMouseCapture { add {} remove {} }
    public event StylusDownEventHandler PreviewStylusDown { add {} remove {} }
    public event StylusDownEventHandler StylusDown { add {} remove {} }
    public event StylusEventHandler PreviewStylusUp { add {} remove {} }
    public event StylusEventHandler StylusUp { add {} remove {} }
    public event StylusEventHandler PreviewStylusMove { add {} remove {} }
    public event StylusEventHandler StylusMove { add {} remove {} }
    public event StylusEventHandler PreviewStylusInAirMove { add {} remove {} }
    public event StylusEventHandler StylusInAirMove { add {} remove {} }
    public event StylusEventHandler StylusEnter { add {} remove {} }
    public event StylusEventHandler StylusLeave { add {} remove {} }
    public event StylusEventHandler PreviewStylusInRange { add {} remove {} }
    public event StylusEventHandler StylusInRange { add {} remove {} }
    public event StylusEventHandler PreviewStylusOutOfRange { add {} remove {} }
    public event StylusEventHandler StylusOutOfRange { add {} remove {} }
    public event StylusSystemGestureEventHandler PreviewStylusSystemGesture { add {} remove {} }
    public event StylusSystemGestureEventHandler StylusSystemGesture { add {} remove {} }
    public event StylusButtonEventHandler StylusButtonDown { add {} remove {} }
    public event StylusButtonEventHandler PreviewStylusButtonDown { add {} remove {} }
    public event StylusButtonEventHandler PreviewStylusButtonUp { add {} remove {} }
    public event StylusButtonEventHandler StylusButtonUp { add {} remove {} }
    public event StylusEventHandler GotStylusCapture { add {} remove {} }
    public event StylusEventHandler LostStylusCapture { add {} remove {} }
    public event KeyEventHandler PreviewKeyDown { add {} remove {} }
    public event KeyEventHandler KeyDown { add {} remove {} }
    public event KeyEventHandler PreviewKeyUp { add {} remove {} }
    public event KeyEventHandler KeyUp { add {} remove {} }
    public event KeyboardFocusChangedEventHandler PreviewGotKeyboardFocus { add {} remove {} }
    public event KeyboardFocusChangedEventHandler GotKeyboardFocus { add {} remove {} }
    public event KeyboardFocusChangedEventHandler PreviewLostKeyboardFocus { add {} remove {} }
    public event KeyboardFocusChangedEventHandler LostKeyboardFocus { add {} remove {} }

    public void AddHandler(RoutedEvent routedEvent, Delegate handler) {}
    public bool CaptureMouse() => false;
    public bool CaptureStylus() => false;
    public bool Focus() => false;
    public void RaiseEvent(RoutedEventArgs e) {}
    public void ReleaseMouseCapture() {}
    public void ReleaseStylusCapture() {}
    public void RemoveHandler(RoutedEvent routedEvent, Delegate handler) {}
}
