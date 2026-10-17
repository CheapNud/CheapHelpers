using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.Barcodes;

public static class BarcodeServiceExtensions
{
    /// <summary>
    /// Registers <see cref="IBarcodeService"/> as a singleton
    /// </summary>
    public static IServiceCollection AddCheapBarcodes(this IServiceCollection services)
    {
        services.AddSingleton<IBarcodeService, BarcodeService>();
        return services;
    }
}
