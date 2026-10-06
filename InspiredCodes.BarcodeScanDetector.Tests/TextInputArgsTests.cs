using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using InspiredCodes.WPF.BarcodeScanDetector;

namespace InspiredCodes.BarcodeScanDetector.Tests;

[TestClass()]
public class TextInputArgsTests
{

    [TestMethod]
    public void TextInputEventArgs_StoresExplicitTimestampAndDelta()
    {
        var args = new TextInputEventArgs("A", 12345L, 678L);

        Assert.AreEqual("A", args.Text);
        Assert.AreEqual(12345L, args.TimestampTicks);
        Assert.AreEqual(678L, args.DeltaToPreviousTicks);
    }

    [TestMethod]
    public void TextInputEventArgs_TwoArgumentConstructor_StoresDeltaAndStampsCurrentTime()
    {
        long before = DateTime.Now.Ticks;
        var args = new TextInputEventArgs("A", 678L);
        long after = DateTime.Now.Ticks;

        Assert.AreEqual(678L, args.DeltaToPreviousTicks);
        Assert.IsTrue(before <= args.TimestampTicks && args.TimestampTicks <= after);
    }

    [TestMethod]
    public void ReturnInputArgs_StoresExplicitTimestampAndDelta()
    {
        var args = new ReturnInputArgs("\r", 12345L, 678L);

        Assert.AreEqual("\r", args.Text);
        Assert.AreEqual(12345L, args.TimestampTicks);
        Assert.AreEqual(678L, args.DeltaToPreviousTicks);
    }

    [TestMethod]
    public void ReturnInputArgs_TwoArgumentConstructor_StoresDeltaAndStampsCurrentTime()
    {
        long before = DateTime.Now.Ticks;
        var args = new ReturnInputArgs("\n", 678L);
        long after = DateTime.Now.Ticks;

        Assert.AreEqual(678L, args.DeltaToPreviousTicks);
        Assert.IsTrue(before <= args.TimestampTicks && args.TimestampTicks <= after);
    }

    [DataTestMethod]
    [DataRow("\r")]
    [DataRow("\n")]
    [DataRow("\r\n")]
    [DataRow("\n\r")]
    public void ReturnInputArgs_AcceptsNewlineText(string text)
    {
        Assert.AreEqual(text, new ReturnInputArgs(text, 0L).Text);
        Assert.AreEqual(text, new ReturnInputArgs(text, 1L, 0L).Text);
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("A")]
    [DataRow("A\r")]
    public void ReturnInputArgs_RejectsNonNewlineText(string text)
    {
        Assert.ThrowsException<ArgumentException>(() => new ReturnInputArgs(text, 0L));
        Assert.ThrowsException<ArgumentException>(() => new ReturnInputArgs(text, 1L, 0L));
    }

    [TestMethod]
    public void DetectorData_InitialPreviousInput_IsEmptyWithNoDelta()
    {
        long before = DateTime.Now.Ticks;
        var data = new DetectorData();
        long after = DateTime.Now.Ticks;

        Assert.AreEqual(string.Empty, data.PreviousInput.Text);
        Assert.IsTrue(before <= data.PreviousInput.TimestampTicks && data.PreviousInput.TimestampTicks <= after);

        // it used to carry the creation timestamp here, because the timestamp was
        // passed where the constructor expects the delta to the previous input
        Assert.AreEqual(0L, data.PreviousInput.DeltaToPreviousTicks);
    }

}
