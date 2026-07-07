using System;
using System.Windows;
using InspiredCodes.WPF.BarcodeScanDetector;

namespace InspiredCodes.BarcodeScanDetector.WpfDemo;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Hook the text input events for the entire window
        this.RegisterTextInput();

        // Subscribe to the barcode scanned event
        ScanDetector.BarcodeScanned += OnBarcodeScanned;
    }

    private void OnBarcodeScanned(object? sender, BarcodeScannedEventArgs e)
    {
        // Update the UI on the dispatcher thread
        Dispatcher.Invoke(() =>
        {
            ResultTextBlock.Text = e.InputText;
        });
    }

    private void SimulateScan_Click(object sender, RoutedEventArgs e)
    {
        // Simulate a barcode scan with a UUIDv7
        string simulatedBarcode = Guid.CreateVersion7().ToString();
        ScanDetector.SimulateBubbleFastInput(simulatedBarcode);
    }
}