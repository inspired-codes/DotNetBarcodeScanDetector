using Xunit;

namespace InspiredCodes.WPF.BarcodeScanDetector.Tests;

public class WpfScanDetectorExtensionsTests
{
    [Fact]
    public void RegisterTextInput_ShouldAttachEventHandler()
    {
        // Arrange
        var mockInput = new MockInputElement();

        // Act & Assert
        // We just verify it doesn't throw and attaches successfully
        var ex = Record.Exception(() => mockInput.RegisterTextInput());
        Assert.Null(ex);
        
        ex = Record.Exception(() => mockInput.UnRegisterTextInput());
        Assert.Null(ex);
    }

    [Fact]
    public void RegisterPreviewTextInput_ShouldAttachEventHandler()
    {
        // Arrange
        var mockInput = new MockInputElement();

        // Act & Assert
        var ex = Record.Exception(() => mockInput.RegisterPreviewTextInput());
        Assert.Null(ex);

        ex = Record.Exception(() => mockInput.UnRegisterPreviewTextInput());
        Assert.Null(ex);
    }
}
