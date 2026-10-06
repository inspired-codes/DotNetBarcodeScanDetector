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

    [Fact]
    public void RegisterTextInput_AttachesOneHandler()
    {
        var mockInput = new MockInputElement();

        mockInput.RegisterTextInput();

        Assert.Equal(1, mockInput.TextInputHandlerCount);
    }

    [Fact]
    public void RegisterTextInput_CalledTwice_StillAttachesOneHandler()
    {
        var mockInput = new MockInputElement();

        mockInput.RegisterTextInput();
        mockInput.RegisterTextInput();

        Assert.Equal(1, mockInput.TextInputHandlerCount);
    }

    [Fact]
    public void UnRegisterTextInput_AfterRegisteringTwice_DetachesEverything()
    {
        var mockInput = new MockInputElement();
        mockInput.RegisterTextInput();
        mockInput.RegisterTextInput();

        mockInput.UnRegisterTextInput();

        Assert.Equal(0, mockInput.TextInputHandlerCount);
    }

    [Fact]
    public void RegisterTextInput_AfterUnRegister_AttachesAgain()
    {
        var mockInput = new MockInputElement();

        mockInput.RegisterTextInput();
        mockInput.UnRegisterTextInput();
        mockInput.RegisterTextInput();

        Assert.Equal(1, mockInput.TextInputHandlerCount);
    }

    [Fact]
    public void UnRegisterTextInput_WhenNeverRegistered_DoesNothing()
    {
        var mockInput = new MockInputElement();

        var ex = Record.Exception(() => mockInput.UnRegisterTextInput());

        Assert.Null(ex);
        Assert.Equal(0, mockInput.TextInputHandlerCount);
    }

    [Fact]
    public void RegisterPreviewTextInput_CalledTwice_StillAttachesOneHandler()
    {
        var mockInput = new MockInputElement();

        mockInput.RegisterPreviewTextInput();
        mockInput.RegisterPreviewTextInput();

        Assert.Equal(1, mockInput.PreviewTextInputHandlerCount);
    }

    [Fact]
    public void UnRegisterPreviewTextInput_AfterRegisteringTwice_DetachesEverything()
    {
        var mockInput = new MockInputElement();
        mockInput.RegisterPreviewTextInput();
        mockInput.RegisterPreviewTextInput();

        mockInput.UnRegisterPreviewTextInput();

        Assert.Equal(0, mockInput.PreviewTextInputHandlerCount);
    }

    [Fact]
    public void TextInputAndPreviewTextInput_AreRegisteredIndependently()
    {
        var mockInput = new MockInputElement();

        mockInput.RegisterTextInput();
        Assert.Equal(1, mockInput.TextInputHandlerCount);
        Assert.Equal(0, mockInput.PreviewTextInputHandlerCount);

        mockInput.RegisterPreviewTextInput();
        mockInput.UnRegisterTextInput();
        Assert.Equal(0, mockInput.TextInputHandlerCount);
        Assert.Equal(1, mockInput.PreviewTextInputHandlerCount);
    }
}
