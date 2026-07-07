@since: 2021-06-28, pmetz@steelcase.com

Barcode Scan Detector

Catches the TextInput Event from Main Window (or any type that implements IInputElement interface)
if input is very fast, like from barcode scanner, the Detector fires a BarcodeScanned event 
containing the concated input text 

Dependencies: needs reference to "PresentationCore", therefore it works with .NET Framework 4.6.1 or later and .NET Core 2.1 or later

Usage with WPF: 
- install the package
- modify MainWindow as below:

    ...
    using InspiredCodes.WPF.BarcodeScanDetector;
    ...
        public partial class MainWindow : Window
        {

            public MainWindow()
            {
                InitializeComponent();
                ScanDetector.Register(this);
            }
        }
    ...

- in the view model (or anywhere in the code) consume the BarcodeScanned event as below:
using InspiredCodes.WPF.BarcodeScanDetector;
...
        public BarcodeViewModel()
        {
            ScanDetector.BarcodeScanned += BarcodeScannedEventHandler;
        }
...
        private void BarcodeScannedEventHandler(object sender, BarcodeScannedArgs e)
        {
            string textOfYourControl = e.InputText;
        }
...

Pushing to steelcase public nuget server:
[nuget version]
{PackageVersion}

[feed]
feed is "EMEA_CAM" (https://steelcase.pkgs.visualstudio.com/_packaging/EMEA_CAM)

[pack]
pack is done automatically on build

[push]
C:\Source\nuget.exe push -Source "EMEA_CAM" -ApiKey AzureDevOps C:\Source\Azure\EMEA_Systems_DEV\tools_BarcodeScanDetector\InspiredCodes.WPF.BarcodeScanDetector\bin\Release\InspiredCodes.WPF.BarcodeScanDetector.2.0.4.nupkg

[delete]
C:\Source\nuget.exe delete -Source "EMEA_CAM" -ApiKey AzureDevOps InspiredCodes.WPF.BarcodeScanDetector {PackageVersion}

[list all versions]
C:\Source\nuget.exe list -AllVersions -Source "EMEA_CAM" InspiredCodes.WPF.BarcodeScanDetector
