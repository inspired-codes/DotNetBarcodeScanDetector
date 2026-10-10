using InspiredCodes.BarcodeScanDetector;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace InspiredCodes.Blazor.BarcodeScanDetector;

public static class BarcodeScanServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="BlazorBarcodeScanService"/> as a scoped service (one per app in Blazor
    /// WebAssembly), with options set by <paramref name="configure"/>.
    /// </summary>
    public static IServiceCollection AddBarcodeScanDetector(this IServiceCollection services, Action<ScanDetectorOptions>? configure = null)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        services.AddScoped(serviceProvider =>
        {
            var options = new ScanDetectorOptions();
            configure?.Invoke(options);
            return new BlazorBarcodeScanService(serviceProvider.GetRequiredService<IJSRuntime>(), options);
        });
        return services;
    }
}
