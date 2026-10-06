using System;

using Xunit;

using InspiredCodes.WPF.BarcodeScanDetector;

namespace InspiredCodes.BarcodeScanDetector.Tests;

public class TextInputArgsTests
{

    [Fact]
    public void TextInputEventArgs_StoresExplicitTimestampAndDelta()
    {
        var args = new TextInputEventArgs("A", 12345L, 678L);

        Assert.Equal("A", args.Text);
        Assert.Equal(12345L, args.TimestampTicks);
        Assert.Equal(678L, args.DeltaToPreviousTicks);
    }

    [Fact]
    public void TextInputEventArgs_TwoArgumentConstructor_StoresDeltaAndStampsCurrentTime()
    {
        long before = DateTime.Now.Ticks;
        var args = new TextInputEventArgs("A", 678L);
        long after = DateTime.Now.Ticks;

        Assert.Equal(678L, args.DeltaToPreviousTicks);
        Assert.True(before <= args.TimestampTicks && args.TimestampTicks <= after);
    }

    [Fact]
    public void ReturnInputArgs_StoresExplicitTimestampAndDelta()
    {
        var args = new ReturnInputArgs("\r", 12345L, 678L);

        Assert.Equal("\r", args.Text);
        Assert.Equal(12345L, args.TimestampTicks);
        Assert.Equal(678L, args.DeltaToPreviousTicks);
    }

    [Fact]
    public void ReturnInputArgs_TwoArgumentConstructor_StoresDeltaAndStampsCurrentTime()
    {
        long before = DateTime.Now.Ticks;
        var args = new ReturnInputArgs("\n", 678L);
        long after = DateTime.Now.Ticks;

        Assert.Equal(678L, args.DeltaToPreviousTicks);
        Assert.True(before <= args.TimestampTicks && args.TimestampTicks <= after);
    }

    [Theory]
    [InlineData("\r")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("\n\r")]
    public void ReturnInputArgs_AcceptsNewlineText(string text)
    {
        Assert.Equal(text, new ReturnInputArgs(text, 0L).Text);
        Assert.Equal(text, new ReturnInputArgs(text, 1L, 0L).Text);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("A\r")]
    public void ReturnInputArgs_RejectsNonNewlineText(string text)
    {
        Assert.Throws<ArgumentException>(() => new ReturnInputArgs(text, 0L));
        Assert.Throws<ArgumentException>(() => new ReturnInputArgs(text, 1L, 0L));
    }

    [Fact]
    public void DetectorData_InitialPreviousInput_IsEmptyWithNoDelta()
    {
        long before = DateTime.Now.Ticks;
        var data = new DetectorData();
        long after = DateTime.Now.Ticks;

        Assert.Equal(string.Empty, data.PreviousInput.Text);
        Assert.True(before <= data.PreviousInput.TimestampTicks && data.PreviousInput.TimestampTicks <= after);

        // it used to carry the creation timestamp here, because the timestamp was
        // passed where the constructor expects the delta to the previous input
        Assert.Equal(0L, data.PreviousInput.DeltaToPreviousTicks);
    }

}
