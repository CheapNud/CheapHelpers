using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.Barcodes;

public static class BarcodeServiceExtensions
{
    /// <summary>
    /// Registers <see cref="IBarcodeService"/> as a singleton
    /// </summary>
    /// <remarks>
    /// The <see cref="IBarcodeService.BarcodeScanned"/> event is app-wide: every subscriber sees every scan.
    /// In a multi-user host such as Blazor Server, filter scans per user and unsubscribe on dispose.
    /// </remarks>
    public static IServiceCollection AddCheapBarcodes(this IServiceCollection services)
    {
        services.AddSingleton<IBarcodeService, BarcodeService>();
        return services;
    }
}
