using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using InspiredCodes.BarcodeScanDetector;
using Xunit;

namespace InspiredCodes.WinForms.BarcodeScanDetector.Tests;

/// <summary>
/// WinForms controls expect an STA thread, which xUnit does not provide, so the control tests run
/// their body on one. Keys are raised the way the framework raises KeyPress.
/// </summary>
public class WinFormsScanDetectorExtensionsTests
{
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

    /// <summary>raises KeyPress the way the framework does when a key is typed</summary>
    private sealed class KeyPressControl : Control
    {
        public void Press(string keys)
        {
            foreach (char key in keys)
                OnKeyPress(new KeyPressEventArgs(key));
        }
    }

    [Fact]
    public void RegisterKeyPress_ForwardsTypedKeysToTheEngine()
    {
        RunOnSta(() =>
        {
            var scans = new List<string>();
            using var control = new KeyPressControl();

            control.RegisterKeyPress(CreateEngine(scans));
            control.Press("ABC\r");

            Assert.Equal(new[] { "ABC" }, scans);
        });
    }

    [Fact]
    public void RegisterKeyPress_CalledTwice_ForwardsEachKeyOnce()
    {
        RunOnSta(() =>
        {
            var scans = new List<string>();
            var engine = CreateEngine(scans);
            using var control = new KeyPressControl();

            control.RegisterKeyPress(engine);
            control.RegisterKeyPress(engine);
            control.Press("ABC\r");

            // a duplicated handler would turn this into "AABBCC"
            Assert.Equal(new[] { "ABC" }, scans);
        });
    }

    [Fact]
    public void UnRegisterKeyPress_AfterRegisteringTwice_StopsForwarding()
    {
        RunOnSta(() =>
        {
            var scans = new List<string>();
            var engine = CreateEngine(scans);
            using var control = new KeyPressControl();
            control.RegisterKeyPress(engine);
            control.RegisterKeyPress(engine);

            control.UnRegisterKeyPress(engine);
            control.Press("ABC\r");

            Assert.Empty(scans);
        });
    }

    [Fact]
    public void RegisterKeyPress_AfterUnRegister_ForwardsAgain()
    {
        RunOnSta(() =>
        {
            var scans = new List<string>();
            var engine = CreateEngine(scans);
            using var control = new KeyPressControl();
            control.RegisterKeyPress(engine);
            control.UnRegisterKeyPress(engine);

            control.RegisterKeyPress(engine);
            control.Press("ABC\r");

            Assert.Equal(new[] { "ABC" }, scans);
        });
    }

    [Fact]
    public void UnRegisterKeyPress_WhenNeverRegistered_DoesNothing()
    {
        RunOnSta(() =>
        {
            var scans = new List<string>();
            using var control = new KeyPressControl();

            var ex = Record.Exception(() => control.UnRegisterKeyPress(CreateEngine(scans)));
            control.Press("ABC\r");

            Assert.Null(ex);
            Assert.Empty(scans);
        });
    }

    [Fact]
    public void TwoEngines_OnOneControl_BothReceiveTheKeys()
    {
        RunOnSta(() =>
        {
            var first = new List<string>();
            var second = new List<string>();
            using var control = new KeyPressControl();

            control.RegisterKeyPress(CreateEngine(first));
            control.RegisterKeyPress(CreateEngine(second));
            control.Press("ABC\r");

            Assert.Equal(new[] { "ABC" }, first);
            Assert.Equal(new[] { "ABC" }, second);
        });
    }

    [Fact]
    public void UnRegisterKeyPress_DetachesOnlyThatEngine()
    {
        RunOnSta(() =>
        {
            var kept = new List<string>();
            var removed = new List<string>();
            var keptEngine = CreateEngine(kept);
            var removedEngine = CreateEngine(removed);
            using var control = new KeyPressControl();
            control.RegisterKeyPress(keptEngine);
            control.RegisterKeyPress(removedEngine);

            control.UnRegisterKeyPress(removedEngine);
            control.Press("ABC\r");

            Assert.Equal(new[] { "ABC" }, kept);
            Assert.Empty(removed);
        });
    }

    [Fact]
    public void RegisterKeyPress_WithoutAnEngine_UsesTheDefaultEngine()
    {
        var scans = new List<string>();
        EventHandler<BarcodeScannedEventArgs> handler = (sender, e) => scans.Add(e.InputText);
        ScanDetector.Reset();
        ScanDetector.BarcodeScanned += handler;
        try
        {
            RunOnSta(() =>
            {
                using var control = new KeyPressControl();
                control.RegisterKeyPress();
                control.Press("ABC\r");
                control.UnRegisterKeyPress();
            });

            Assert.Equal(new[] { "ABC" }, scans);
        }
        finally
        {
            // the facade's engine is process-wide
            ScanDetector.BarcodeScanned -= handler;
            ScanDetector.Reset();
        }
    }

    private const int WM_KEYDOWN = 0x0100;
    private const int WM_CHAR = 0x0102;

    private static bool[] Send(BarcodeScanMessageFilter filter, int msg, string keys)
    {
        var passedOn = new List<bool>();
        foreach (char key in keys)
        {
            var m = Message.Create(IntPtr.Zero, msg, new IntPtr(key), IntPtr.Zero);
            passedOn.Add(!filter.PreFilterMessage(ref m));
        }
        return passedOn.ToArray();
    }

    [Fact]
    public void MessageFilter_FeedsCharactersToTheEngine_AndNeverSwallowsThem()
    {
        var scans = new List<string>();
        var filter = new BarcodeScanMessageFilter(CreateEngine(scans));

        bool[] passedOn = Send(filter, WM_CHAR, "ABC\r");

        Assert.Equal(new[] { "ABC" }, scans);
        Assert.All(passedOn, Assert.True);
    }

    [Fact]
    public void MessageFilter_IgnoresOtherMessages()
    {
        var scans = new List<string>();
        var filter = new BarcodeScanMessageFilter(CreateEngine(scans));

        bool[] passedOn = Send(filter, WM_KEYDOWN, "ABC\r");

        Assert.Empty(scans);
        Assert.All(passedOn, Assert.True);
    }

    [Fact]
    public void MessageFilter_WithoutAnEngine_UsesTheDefaultEngine()
    {
        Assert.Same(ScanDetector.Default, new BarcodeScanMessageFilter().Engine);
    }
}
