using CheapHelpers.Services.Geocoding.Configuration;
using CheapHelpers.Services.Geocoding.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Services.Geocoding;

/// <summary>
/// Factory for creating geocoding service instances
/// </summary>
public class GeocodingServiceFactory : IGeocodingServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly GeocodingConfiguration _configuration;
    private readonly ILogger<GeocodingServiceFactory> _logger;

    public GeocodingServiceFactory(
        IServiceProvider serviceProvider,
        GeocodingConfiguration configuration,
        ILogger<GeocodingServiceFactory> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get the default geocoding service (configured via DefaultProvider)
    /// </summary>
    public IGeocodingService GetDefaultService()
    {
        _logger.LogDebug("Getting default geocoding service: {Provider}", _configuration.DefaultProvider);
        return GetService(_configuration.DefaultProvider);
    }

    /// <summary>
    /// Get a specific geocoding provider
    /// </summary>
    public IGeocodingService GetService(GeocodingProvider provider)
    {
        _logger.LogDebug("Getting geocoding service for provider: {Provider}", provider);

        return provider switch
        {
            GeocodingProvider.Mapbox => _serviceProvider.GetRequiredService<MapboxGeocodingService>(),
            GeocodingProvider.AzureMaps => _serviceProvider.GetRequiredService<AzureMapsGeocodingService>(),
            GeocodingProvider.GoogleMaps => _serviceProvider.GetRequiredService<GoogleMapsGeocodingService>(),
            GeocodingProvider.PtvMaps => _serviceProvider.GetRequiredService<PtvMapsGeocodingService>(),
            _ => throw new ArgumentException($"Unknown geocoding provider: {provider}", nameof(provider))
        };
    }
}
