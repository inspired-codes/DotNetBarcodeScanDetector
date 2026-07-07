using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using InspiredCodes.WPF.BarcodeScanDetector;
using InspiredCodes.BarcodeScanDetector.Tests.Mock;

namespace InspiredCodes.BarcodeScanDetector.Tests;

[TestClass()]
public class ScanDetectorTests
{


    public Queue<string> ScannedBarcodes { get; set; }
    public Queue<string> PreviewScannedBarcodes { get; set; }
    public Stopwatch Stopwatch { get; set; }

    [TestInitialize]
    public void InitializeTest()
    {
        ScanDetector.Reset();
        Stopwatch = new Stopwatch();
        ScannedBarcodes = new Queue<string>();
        PreviewScannedBarcodes = new Queue<string>();
    }
    [TestMethod]
    public void SimulateFastInputTest()
    {

        ScanDetector.BarcodeScanned += TextInputHandler;
        ScanDetector.PreviewBarcodeScanned += PreviewTextInputHandler;

        var barcodes = new SomeBarcodes();
        foreach (string barcode in barcodes.ToArray())
        {
            Stopwatch.Restart();
            ScanDetector.SimulateBubbleFastInput(barcode);
            ScanDetector.SimulateTunnelFastInput(barcode);
            Stopwatch.Stop();
            Debug.WriteLine($"  loop,elapsed: {Stopwatch.ElapsedMilliseconds}");
            System.Threading.Thread.Sleep(350); // Allow the 300ms scan cooldown to expire
        }

        string result, previewResult;
        foreach (string barcode in barcodes)
        {
            result = ScannedBarcodes.Dequeue();
            previewResult = PreviewScannedBarcodes.Dequeue();
            
            Assert.AreEqual(barcode, result);
            Assert.AreEqual(barcode, previewResult);
        }
        Assert.IsTrue(0 == ScannedBarcodes.Count);


    }
    [TestMethod]
    public void BufferLimitTest()
    {
        ScanDetector.BarcodeScanned += TextInputHandler;

        // 1. Send a successful scan (under 4096)
        ScanDetector.SimulateBubbleFastInput("OK");
        Assert.AreEqual(1, ScannedBarcodes.Count);
        Assert.AreEqual("OK", ScannedBarcodes.Dequeue());

        // This successful scan triggers a 300ms cooldown.
        // 2. Send another scan immediately (should be discarded due to cooldown)
        ScanDetector.SimulateBubbleFastInput("FAIL");
        Assert.AreEqual(0, ScannedBarcodes.Count);

        // 3. Wait for 350ms to let cooldown expire
        System.Threading.Thread.Sleep(350);

        // 4. Send a scan exceeding 4096
        string input = new string('A', 4100);
        ScanDetector.SimulateBubbleFastInput(input);
        Assert.AreEqual(0, ScannedBarcodes.Count); // Discarded on limit, starts cooldown

        // 5. Send a scan during cooldown (extends it)
        ScanDetector.SimulateBubbleFastInput("B");
        Assert.AreEqual(0, ScannedBarcodes.Count); // Discarded, cooldown extended

        // 6. Wait for 150ms (cooldown still active because it was extended)
        System.Threading.Thread.Sleep(150);
        ScanDetector.SimulateBubbleFastInput("C"); // Discarded, cooldown extended again
        Assert.AreEqual(0, ScannedBarcodes.Count);

        // 7. Wait 350ms to let cooldown expire
        System.Threading.Thread.Sleep(350);

        // 8. Send a scan now
        ScanDetector.SimulateBubbleFastInput("SUCCESS");
        Assert.AreEqual(1, ScannedBarcodes.Count);
        Assert.AreEqual("SUCCESS", ScannedBarcodes.Dequeue());

        ScanDetector.BarcodeScanned -= TextInputHandler;
    }
    void TextInputHandler(object sender, BarcodeScannedEventArgs e)
    {
        ScannedBarcodes.Enqueue(e.InputText);
        Debug.WriteLine($"  handler,elapsed: {Stopwatch.ElapsedMilliseconds}");
    }
    void PreviewTextInputHandler(object sender, BarcodeScannedEventArgs e)
    {
        PreviewScannedBarcodes.Enqueue(e.InputText);
        Debug.WriteLine($"  handler,elapsed: {Stopwatch.ElapsedMilliseconds}");
    }

}