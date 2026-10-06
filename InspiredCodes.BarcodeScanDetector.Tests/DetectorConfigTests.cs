using System;

using Xunit;

using InspiredCodes.WPF.BarcodeScanDetector;

namespace InspiredCodes.BarcodeScanDetector.Tests;

public class DetectorConfigTests
{

    [Fact]
    public void NewLineRN_IsCarriageReturnThenLineFeed()
    {
        Assert.Equal("\r\n", DetectorConfig.NewLineRN);
    }

    [Theory]
    [InlineData("\r")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("\n\r")]
    public void IsLineFeedOrCarriageReturn_AcceptsNewlineSequences(string text)
    {
        Assert.True(DetectorConfig.IsLineFeedOrCarriageReturn(text));
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("A\r")]
    [InlineData("\r\r")]
    [InlineData("\r\n\r\n")]
    public void IsLineFeedOrCarriageReturn_RejectsEverythingElse(string text)
    {
        Assert.False(DetectorConfig.IsLineFeedOrCarriageReturn(text));
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("1_C04441_R042")]
    [InlineData("tab\there")]
    public void CheckNoCrOrLf_AllowsTextWithoutLineEnds(string text)
    {
        DetectorConfig.CheckNoCrOrLf(text);
    }

    [Theory]
    [InlineData("\r")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("ABC\r")]
    [InlineData("ABC\n")]
    [InlineData("A\rB")]
    [InlineData("A\nB")]
    [InlineData("\rABC")]
    public void CheckNoCrOrLf_ThrowsWhenTextContainsLineEnd(string text)
    {
        Assert.Throws<ArgumentException>(() => DetectorConfig.CheckNoCrOrLf(text));
    }

}
