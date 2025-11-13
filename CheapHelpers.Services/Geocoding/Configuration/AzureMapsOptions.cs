namespace CheapHelpers.Services.Geocoding.Configuration;

/// <summary>
/// Configuration options for Azure Maps Search API
/// </summary>
public class AzureMapsOptions
{
    /// <summary>
    /// Azure Maps subscription key
    /// Get yours at: https://portal.azure.com/
    /// </summary>
    public string SubscriptionKey { get; set; } = string.Empty;

    /// <summary>
    /// Azure Maps client ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Azure Maps endpoint URL
    /// Default: https://atlas.microsoft.com/search
    /// </summary>
    public string Endpoint { get; set; } = "https://atlas.microsoft.com/search";

    /// <summary>
    /// Optional timeout for HTTP requests (in seconds)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
