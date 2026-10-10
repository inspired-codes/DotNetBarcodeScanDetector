# Barcode Scan Detector

A lightweight .NET library designed to distinguish between manual keyboard typing and rapid input sequence from physical barcode scanners (which emulate keyboard keystrokes). It measures the time elapsed between keystrokes to detect fast bursts of characters and fires event callbacks.

---

## Projects in the Solution

The solution is split into separate projects for a clean, decoupled architecture:

*   **[InspiredCodes.BarcodeScanDetector](file:///c:/Users/Peter/Source/Repos/BarcodeScanDetector/InspiredCodes.BarcodeScanDetector)** (Core Library)
    *   Contains the hardware-agnostic timing buffers, the 4096-character safety limit, and the 300ms debounced cooldown logic. Target: `.NET Standard 2.0/2.1`.
*   **[InspiredCodes.WPF.BarcodeScanDetector](file:///c:/Users/Peter/Source/Repos/BarcodeScanDetector/InspiredCodes.WPF.BarcodeScanDetector)** (WPF Extensions)
    *   A wrapper that hooks into WPF's `TextInput` and `PreviewTextInput` events for easy registration.
*   **[InspiredCodes.WinForms.BarcodeScanDetector](file:///c:/Users/Peter/Source/Repos/BarcodeScanDetector/InspiredCodes.WinForms.BarcodeScanDetector)** (WinForms Extensions)
    *   A wrapper that hooks into Windows Forms' `KeyPress` events.
*   **[InspiredCodes.Blazor.BarcodeScanDetector](file:///c:/Users/Peter/Source/Repos/BarcodeScanDetector/InspiredCodes.Blazor.BarcodeScanDetector)** (Blazor Extensions)
    *   A wrapper that provides a `BarcodeScanListener` component and `BlazorBarcodeScanService` for Blazor WebAssembly applications.
*   **[InspiredCodes.BarcodeScanDetector.WpfDemo](file:///c:/Users/Peter/Source/Repos/BarcodeScanDetector/InspiredCodes.BarcodeScanDetector.WpfDemo)**
    *   A simple WPF demo application showcasing scan listening and debugging with a UUIDv7 fast input simulator.
*   **[InspiredCodes.BarcodeScanDetector.WinFormsDemo](file:///c:/Users/Peter/Source/Repos/BarcodeScanDetector/InspiredCodes.BarcodeScanDetector.WinFormsDemo)**
    *   A simple WinForms demo application demonstrating scan listening and a UUIDv7 fast input simulator.
*   **[InspiredCodes.BarcodeScanDetector.BlazorPwaDemo](file:///c:/Users/Peter/Source/Repos/BarcodeScanDetector/InspiredCodes.BarcodeScanDetector.BlazorPwaDemo)**
    *   A Blazor WebAssembly PWA demo application showcasing barcode scan interception within a web browser.
*   **Tests Projects**
    *   `InspiredCodes.BarcodeScanDetector.Tests`, `InspiredCodes.WPF.BarcodeScanDetector.Tests`, `InspiredCodes.WinForms.BarcodeScanDetector.Tests`, and `InspiredCodes.Blazor.BarcodeScanDetector.Tests` cover the core logic, event registrations, and the 4096-character cooldown limits.

---

## State Machine Documentation (BPMN)

The underlying timing and cooldown state machine is documented in a BPMN file:
*   [CharInputStateMachine.bpmn](file:///c:/Users/Peter/Source/Repos/BarcodeScanDetector/Documentation/CharInputStateMachine.bpmn)

### How to View the Diagram
You can view or edit this diagram in two ways:
1.  **Online Viewer (Zero Install):** Open **[bpmn.io (online modeler)](https://demo.bpmn.io/)** and drag-and-drop the `CharInputStateMachine.bpmn` file directly into your browser window.
2.  **Desktop Client:** Download and open the file using **[Camunda Modeler](https://camunda.com/download/modeler/)**.

---

## Getting Started (WPF Example)

1.  Add the **InspiredCodes.WPF.BarcodeScanDetector** NuGet package to your project.
2.  Register the input listener on your main window in the constructor:
    ```csharp
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // Register window-level text input listening
            this.RegisterTextInput();
        }
    }
    ```
3.  Subscribe to the `BarcodeScanned` event:
    ```csharp
    ScanDetector.BarcodeScanned += (sender, e) =>
    {
        // Access the scanned text
        string barcodeValue = e.InputText;
        Console.WriteLine($"Scanned: {barcodeValue}");
    };
    ```

For unregistering, use `this.UnRegisterTextInput()`.

---
**Author:** Peter Metz
**Contributors:** Steven Lee