namespace CheapHelpers.Blazor.Hybrid.Abstractions;

/// <summary>
/// Generic WebView bridge for extracting data from JavaScript storage
/// Supports localStorage, sessionStorage, cookies, and DOM scraping
/// </summary>
/// <typeparam name="TData">The type of data to extract from the WebView</typeparam>
public interface IWebViewBridge<TData> where TData : class
{
    /// <summary>
    /// Extract data from WebView storage by key
    /// </summary>
    Task<TData?> ExtractDataAsync(string key);

    /// <summary>
    /// Get all data from a specific storage type
    /// </summary>
    Task<Dictionary<string, string>> GetAllStorageAsync(StorageType storageType);

    /// <summary>
    /// Monitor storage for changes and trigger event when data changes
    /// </summary>
    Task StartMonitoringAsync(TimeSpan pollingInterval);

    /// <summary>
    /// Stop monitoring storage for changes
    /// </summary>
    void StopMonitoring();

    /// <summary>
    /// Event triggered when monitored data changes
    /// </summary>
    event Action<TData?>? DataChanged;
}

/// <summary>
/// Storage types supported by the WebView bridge
/// </summary>
public enum StorageType
{
    LocalStorage,
    SessionStorage,
    Cookies
}
