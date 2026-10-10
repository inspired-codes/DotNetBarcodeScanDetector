using System;
using System.Drawing;
using System.Windows.Forms;
using InspiredCodes.WinForms.BarcodeScanDetector;

namespace InspiredCodes.BarcodeScanDetector.WinFormsDemo;

public partial class Form1 : Form
{
    private Label resultLabel;
    private Button simulateButton;

    public Form1()
    {
        InitializeComponent();
        SetupUI();

        // Required for the form to receive key events before they reach controls
        this.KeyPreview = true;

        // Hook the key press events for the entire form
        this.RegisterKeyPress();

        // Subscribe to the barcode scanned event
        ScanDetector.BarcodeScanned += OnBarcodeScanned;
    }

    private void SetupUI()
    {
        this.Text = "WinForms Barcode Demo";
        this.Size = new Size(400, 250);

        Label titleLabel = new Label
        {
            Text = "Last Scanned Barcode:",
            Font = new Font(this.Font, FontStyle.Bold),
            Location = new Point(20, 20),
            AutoSize = true
        };

        resultLabel = new Label
        {
            Text = "Waiting for scan...",
            ForeColor = Color.Blue,
            Location = new Point(20, 50),
            AutoSize = true
        };

        simulateButton = new Button
        {
            Text = "Simulate Scan (UUIDv7)",
            Location = new Point(20, 100),
            Size = new Size(200, 40)
        };
        simulateButton.Click += SimulateScan_Click;

        this.Controls.Add(titleLabel);
        this.Controls.Add(resultLabel);
        this.Controls.Add(simulateButton);
    }

    private void OnBarcodeScanned(object? sender, BarcodeScannedEventArgs e)
    {
        // Update the UI on the main thread
        if (this.InvokeRequired)
        {
            this.Invoke(new Action(() => resultLabel.Text = e.InputText));
        }
        else
        {
            resultLabel.Text = e.InputText;
        }
    }

    private void SimulateScan_Click(object? sender, EventArgs e)
    {
        // Simulate a barcode scan with a UUIDv7
        string simulatedBarcode = Guid.CreateVersion7().ToString();
        ScanDetector.SimulateBubbleFastInput(simulatedBarcode);
    }
}
