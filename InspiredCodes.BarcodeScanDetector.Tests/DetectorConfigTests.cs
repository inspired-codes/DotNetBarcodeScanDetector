using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using InspiredCodes.WPF.BarcodeScanDetector;

namespace InspiredCodes.BarcodeScanDetector.Tests;

[TestClass()]
public class DetectorConfigTests
{

    [TestMethod]
    public void NewLineRN_IsCarriageReturnThenLineFeed()
    {
        Assert.AreEqual("\r\n", DetectorConfig.NewLineRN);
    }

    [DataTestMethod]
    [DataRow("\r")]
    [DataRow("\n")]
    [DataRow("\r\n")]
    [DataRow("\n\r")]
    public void IsLineFeedOrCarriageReturn_AcceptsNewlineSequences(string text)
    {
        Assert.IsTrue(DetectorConfig.IsLineFeedOrCarriageReturn(text));
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("A")]
    [DataRow(" ")]
    [DataRow("\t")]
    [DataRow("A\r")]
    [DataRow("\r\r")]
    [DataRow("\r\n\r\n")]
    public void IsLineFeedOrCarriageReturn_RejectsEverythingElse(string text)
    {
        Assert.IsFalse(DetectorConfig.IsLineFeedOrCarriageReturn(text));
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("A")]
    [DataRow("1_C04441_R042")]
    [DataRow("tab\there")]
    public void CheckNoCrOrLf_AllowsTextWithoutLineEnds(string text)
    {
        DetectorConfig.CheckNoCrOrLf(text);
    }

    [DataTestMethod]
    [DataRow("\r")]
    [DataRow("\n")]
    [DataRow("\r\n")]
    [DataRow("ABC\r")]
    [DataRow("ABC\n")]
    [DataRow("A\rB")]
    [DataRow("A\nB")]
    [DataRow("\rABC")]
    public void CheckNoCrOrLf_ThrowsWhenTextContainsLineEnd(string text)
    {
        Assert.ThrowsException<ArgumentException>(() => DetectorConfig.CheckNoCrOrLf(text));
    }

}
