using CheapHelpers.Services.Geocoding.Configuration;
using CheapHelpers.Services.Geocoding.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.Services.Geocoding.Extensions;

/// <summary>
/// Extension methods for registering geocoding services
/// </summary>
public static class GeocodingServiceCollectionExtensions
{
    /// <summary>
    /// Add geocoding services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Configuration action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddGeocodingServices(
        this IServiceCollection services,
        Action<GeocodingConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        // Create and configure the configuration object
        var configuration = new GeocodingConfiguration();
        configure(configuration);

        // Register configuration as singleton
        services.AddSingleton(configuration);

        // Register individual provider options
        services.AddSingleton(configuration.Mapbox);
        services.AddSingleton(configuration.AzureMaps);
        services.AddSingleton(configuration.GoogleMaps);
        services.AddSingleton(configuration.PtvMaps);

        // Register HttpClient for Mapbox
        services.AddHttpClient<MapboxGeocodingService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(configuration.Mapbox.TimeoutSeconds);
        });

        // Register HttpClient for Azure Maps
        services.AddHttpClient<AzureMapsGeocodingService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(configuration.AzureMaps.TimeoutSeconds);
        });

        // Register HttpClient for PTV Maps
        services.AddHttpClient<PtvMapsGeocodingService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(configuration.PtvMaps.TimeoutSeconds);
        });

        // Register provider services as transient
        services.AddTransient<MapboxGeocodingService>();
        services.AddTransient<AzureMapsGeocodingService>();
        services.AddTransient<GoogleMapsGeocodingService>();
        services.AddTransient<PtvMapsGeocodingService>();

        // Register factory as singleton
        services.AddSingleton<IGeocodingServiceFactory, GeocodingServiceFactory>();

        // Register default service as IGeocodingService
        services.AddTransient<IGeocodingService>(sp =>
        {
            var factory = sp.GetRequiredService<IGeocodingServiceFactory>();
            return factory.GetDefaultService();
        });

        return services;
    }

    /// <summary>
    /// Add geocoding services with default configuration (Mapbox as default provider)
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddGeocodingServices(this IServiceCollection services)
    {
        return AddGeocodingServices(services, _ => { });
    }
}
