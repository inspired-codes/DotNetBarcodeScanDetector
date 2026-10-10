using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using InspiredCodes.BarcodeScanDetector;
using Xunit;

namespace InspiredCodes.WPF.BarcodeScanDetector.Tests;

/// <summary>
/// WPF elements need an STA thread, which xUnit does not provide, so every test runs its body on
/// one. Input is raised as real routed TextInput / PreviewTextInput events.
/// </summary>
public class WpfScanDetectorExtensionsTests
{
    private static readonly RoutedEvent TextInput = TextCompositionManager.TextInputEvent;
    private static readonly RoutedEvent PreviewTextInput = TextCompositionManager.PreviewTextInputEvent;

    /// <summary>an engine of its own, so tests don't share state; keys raised back to back are always fast</summary>
    private static ScanDetectorEngine CreateEngine(List<string> scans)
    {
        var engine = new ScanDetectorEngine(new ScanDetectorOptions { InterKeyThreshold = TimeSpan.FromSeconds(1) });
        engine.BarcodeScanned += (sender, e) => scans.Add(e.InputText);
        return engine;
    }

    private static void RunOnSta(Action body)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                body();
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (error != null)
            ExceptionDispatchInfo.Capture(error).Throw();
    }

    /// <summary>raises <paramref name="routedEvent"/> on <paramref name="target"/> once per character, like typing</summary>
    private static void Type(UIElement target, RoutedEvent routedEvent, string keys)
    {
        foreach (char key in keys)
        {
            var args = new TextCompositionEventArgs(Keyboard.PrimaryDevice, new TextComposition(InputManager.Current, target, key.ToString()))
            {
                RoutedEvent = routedEvent,
            };
            target.RaiseEvent(args);
        }
    }

    [Fact]
    public void RegisterTextInput_ForwardsTypedTextToTheEngine()
    {
        RunOnSta(() =>
        {
            var scans = new List<string>();
            var element = new Border();

            element.RegisterTextInput(CreateEngine(scans));
            Type(element, TextInput, "ABC\r");

            Assert.Equal(new[] { "ABC" }, scans);
        });
    }

    [Fact]
    public void RegisterTextInput_CalledTwice_ForwardsEachInputOnce()
    {
        RunOnSta(() =>
        {
            var scans = new List<string>();
            var engine = CreateEngine(scans);
            var element = new Border();

            element.RegisterTextInput(engine);
            element.RegisterTextInput(engine);
            Type(element, TextInput, "ABC\r");

            // a duplicated handler would turn this into "AABBCC"
            Assert.Equal(new[] { "ABC" }, scans);
        });
    }

    [Fact]
    public void UnRegisterTextInput_AfterRegisteringTwice_StopsForwarding()
    {
        RunOnSta(() =>
        {
            var scans = new List<string>();
            var engine = CreateEngine(scans);
            var element = new Border();
            element.RegisterTextInput(engine);
            element.RegisterTextInput(engine);

            element.UnRegisterTextInput(engine);
            Type(element, TextInput, "ABC\r");

            Assert.Empty(scans);
        });
    }

    [Fact]
    public void RegisterTextInput_AfterUnRegister_ForwardsAgain()
    {
        RunOnSta(() =>
        {
            var scans = new List<string>();
            var engine = CreateEngine(scans);
            var element = new Border();
            element.RegisterTextInput(engine);
            element.UnRegisterTextInput(engine);

            element.RegisterTextInput(engine);
            Type(element, TextInput, "ABC\r");

            Assert.Equal(new[] { "ABC" }, scans);
        });
    }

    [Fact]
    public void UnRegisterTextInput_WhenNeverRegistered_DoesNothing()
    {
        RunOnSta(() =>
        {
            var element = new Border();

            var ex = Record.Exception(() => element.UnRegisterTextInput(CreateEngine(new List<string>())));

            Assert.Null(ex);
        });
    }

    [Fact]
    public void InputMarkedHandledByAChild_IsStillSeen()
    {
        RunOnSta(() =>
        {
            var scans = new List<string>();
            var child = new Border();
            var parent = new Border { Child = child };
            // what a focused TextBox does with the text it receives
            child.AddHandler(TextInput, new TextCompositionEventHandler((sender, e) => e.Handled = true));
            int plainHandlerCalls = 0;
            parent.AddHandler(TextInput, new TextCompositionEventHandler((sender, e) => plainHandlerCalls++));

            parent.RegisterTextInput(CreateEngine(scans));
            Type(child, TextInput, "ABC\r");

            Assert.Equal(new[] { "ABC" }, scans);
            // the input really was handled on the way: an ordinary handler on the parent never saw it
            Assert.Equal(0, plainHandlerCalls);
        });
    }

    [Fact]
    public void TwoEngines_OnOneElement_BothReceiveTheInput()
    {
        RunOnSta(() =>
        {
            var first = new List<string>();
            var second = new List<string>();
            var element = new Border();

            element.RegisterTextInput(CreateEngine(first));
            element.RegisterTextInput(CreateEngine(second));
            Type(element, TextInput, "ABC\r");

            Assert.Equal(new[] { "ABC" }, first);
            Assert.Equal(new[] { "ABC" }, second);
        });
    }

    [Fact]
    public void UnRegisterTextInput_DetachesOnlyThatEngine()
    {
        RunOnSta(() =>
        {
            var kept = new List<string>();
            var removed = new List<string>();
            var keptEngine = CreateEngine(kept);
            var removedEngine = CreateEngine(removed);
            var element = new Border();
            element.RegisterTextInput(keptEngine);
            element.RegisterTextInput(removedEngine);

            element.UnRegisterTextInput(removedEngine);
            Type(element, TextInput, "ABC\r");

            Assert.Equal(new[] { "ABC" }, kept);
            Assert.Empty(removed);
        });
    }

    [Fact]
    public void RegisterPreviewTextInput_ListensToThePreviewEventOnly()
    {
        RunOnSta(() =>
        {
            var scans = new List<string>();
            var element = new Border();
            element.RegisterPreviewTextInput(CreateEngine(scans));

            Type(element, TextInput, "ONE\r");
            Type(element, PreviewTextInput, "TWO\r");

            Assert.Equal(new[] { "TWO" }, scans);
        });
    }

    [Fact]
    public void WithoutAnEngine_TheProcessWideEnginesAreUsed()
    {
        var bubble = new List<string>();
        var tunnel = new List<string>();
        EventHandler<BarcodeScannedEventArgs> bubbleHandler = (sender, e) => bubble.Add(e.InputText);
        EventHandler<BarcodeScannedEventArgs> tunnelHandler = (sender, e) => tunnel.Add(e.InputText);
        ScanDetector.Reset();
        ScanDetector.BarcodeScanned += bubbleHandler;
        ScanDetector.PreviewBarcodeScanned += tunnelHandler;
        try
        {
            RunOnSta(() =>
            {
                var element = new Border();
                element.RegisterTextInput();
                element.RegisterPreviewTextInput();

                Type(element, TextInput, "ONE\r");
                Type(element, PreviewTextInput, "TWO\r");

                element.UnRegisterTextInput();
                element.UnRegisterPreviewTextInput();
            });

            Assert.Equal(new[] { "ONE" }, bubble);
            Assert.Equal(new[] { "TWO" }, tunnel);
        }
        finally
        {
            // the facade's engines are process-wide
            ScanDetector.BarcodeScanned -= bubbleHandler;
            ScanDetector.PreviewBarcodeScanned -= tunnelHandler;
            ScanDetector.Reset();
        }
    }
}
