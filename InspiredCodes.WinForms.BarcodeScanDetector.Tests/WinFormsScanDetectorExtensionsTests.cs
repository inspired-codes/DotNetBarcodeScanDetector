using System.Windows.Forms;
using InspiredCodes.WPF.BarcodeScanDetector;
using Xunit;

namespace InspiredCodes.WinForms.BarcodeScanDetector.Tests;

/// <summary>
/// The detector behind the extensions is static, so every test starts from a reset detector,
/// widens the fast-input threshold (typing is simulated, timing must not matter) and puts
/// everything back afterwards.
/// </summary>
public class WinFormsScanDetectorExtensionsTests : IDisposable
{
    private readonly long _savedThresholdTicks;
    private readonly List<string> _scans = new();
    private readonly EventHandler<BarcodeScannedEventArgs> _handler;

    public WinFormsScanDetectorExtensionsTests()
    {
        _savedThresholdTicks = DetectorConfig.ThresholdTicks;
        DetectorConfig.ThresholdMillisec = 1000;

        ScanDetector.Reset();
        _handler = (sender, e) => _scans.Add(e.InputText);
        ScanDetector.BarcodeScanned += _handler;
    }

    public void Dispose()
    {
        ScanDetector.BarcodeScanned -= _handler;
        DetectorConfig.ThresholdTicks = _savedThresholdTicks;
        ScanDetector.Reset();
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
    public void RegisterKeyPress_ShouldAttachEventHandler()
    {
        // Arrange
        using var control = new Control();

        // Act & Assert
        // Verify that calling Register/Unregister does not throw exceptions
        var ex = Record.Exception(() => control.RegisterKeyPress());
        Assert.Null(ex);

        ex = Record.Exception(() => control.UnRegisterKeyPress());
        Assert.Null(ex);
    }

    [Fact]
    public void RegisterKeyPress_ForwardsTypedKeysToTheDetector()
    {
        using var control = new KeyPressControl();
        control.RegisterKeyPress();

        control.Press("ABC\r");

        Assert.Equal(new[] { "ABC" }, _scans);
    }

    [Fact]
    public void RegisterKeyPress_CalledTwice_ForwardsEachKeyOnce()
    {
        using var control = new KeyPressControl();
        control.RegisterKeyPress();
        control.RegisterKeyPress();

        control.Press("ABC\r");

        // a duplicated handler would turn this into "AABBCC"
        Assert.Equal(new[] { "ABC" }, _scans);
    }

    [Fact]
    public void UnRegisterKeyPress_AfterRegisteringTwice_StopsForwarding()
    {
        using var control = new KeyPressControl();
        control.RegisterKeyPress();
        control.RegisterKeyPress();

        control.UnRegisterKeyPress();
        control.Press("ABC\r");

        Assert.Empty(_scans);
    }

    [Fact]
    public void RegisterKeyPress_AfterUnRegister_ForwardsAgain()
    {
        using var control = new KeyPressControl();
        control.RegisterKeyPress();
        control.UnRegisterKeyPress();
        control.RegisterKeyPress();

        control.Press("ABC\r");

        Assert.Equal(new[] { "ABC" }, _scans);
    }

    [Fact]
    public void UnRegisterKeyPress_WhenNeverRegistered_DoesNothing()
    {
        using var control = new KeyPressControl();

        var ex = Record.Exception(() => control.UnRegisterKeyPress());
        control.Press("ABC\r");

        Assert.Null(ex);
        Assert.Empty(_scans);
    }
}
