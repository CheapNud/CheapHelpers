namespace CheapHelpers.Services.Geocoding.Configuration;

/// <summary>
/// Configuration options for PTV Maps Geocoding API
/// </summary>
public class PtvMapsOptions
{
    /// <summary>
    /// PTV Maps API key
    /// Get yours at: https://developer.myptv.com/
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Optional timeout for HTTP requests (in seconds)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
