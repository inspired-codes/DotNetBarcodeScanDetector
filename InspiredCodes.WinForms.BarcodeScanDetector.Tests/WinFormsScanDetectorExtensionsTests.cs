using System.Windows.Forms;
using Xunit;

namespace InspiredCodes.WinForms.BarcodeScanDetector.Tests;

public class WinFormsScanDetectorExtensionsTests
{
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
}
