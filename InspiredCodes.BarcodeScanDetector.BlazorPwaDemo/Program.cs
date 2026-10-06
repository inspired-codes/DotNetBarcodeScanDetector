using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using InspiredCodes.BarcodeScanDetector.BlazorPwaDemo;
using InspiredCodes.Blazor.BarcodeScanDetector;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// one scan detector for the app; pass options here, e.g. options => options.Terminators = ScanTerminators.Enter | ScanTerminators.Tab
builder.Services.AddBarcodeScanDetector();

await builder.Build().RunAsync();
