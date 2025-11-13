namespace CheapHelpers.Services.Geocoding.Configuration;

/// <summary>
/// Configuration options for Google Maps Geocoding API
/// </summary>
public class GoogleMapsOptions
{
    /// <summary>
    /// Google Maps API key
    /// Get yours at: https://console.cloud.google.com/apis/credentials
    /// Enable the Geocoding API for your project
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Optional timeout for HTTP requests (in seconds)
    /// Note: GoogleApi library handles HTTP internally
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
