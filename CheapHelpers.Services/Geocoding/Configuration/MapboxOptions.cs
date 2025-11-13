namespace CheapHelpers.Services.Geocoding.Configuration;

/// <summary>
/// Configuration options for Mapbox Geocoding API
/// </summary>
public class MapboxOptions
{
    /// <summary>
    /// Mapbox access token
    /// Get yours at: https://account.mapbox.com/access-tokens/
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Optional timeout for HTTP requests (in seconds)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
