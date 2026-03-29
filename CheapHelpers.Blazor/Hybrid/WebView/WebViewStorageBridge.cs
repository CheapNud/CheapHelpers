using System.Diagnostics;
using CheapHelpers.Blazor.Hybrid.Abstractions;
using Microsoft.JSInterop;

namespace CheapHelpers.Blazor.Hybrid.WebView;

/// <summary>
/// Default implementation of <see cref="IWebViewBridge{TData}"/> that extracts typed data
/// from WebView storage (localStorage, sessionStorage, cookies) via JS interop.
/// Supports periodic polling for change detection.
/// </summary>
/// <typeparam name="TData">The type of data to extract and deserialize from storage.</typeparam>
public class WebViewStorageBridge<TData>(
    IJSRuntime jsRuntime,
    WebViewStorageBridgeConfig config) : IWebViewBridge<TData>, IDisposable where TData : class
{
    private CancellationTokenSource? _monitoringCts;
    private TData? _lastKnownData;
    private bool _disposed;

    public event Action<TData?>? DataChanged;

    public async Task<TData?> ExtractDataAsync(string key)
    {
        try
        {
            var storageType = config.DefaultStorageType;
            var rawJson = await ReadStorageValueAsync(key, storageType);
            return WebViewJsonParser.ParseJson<TData>(rawJson);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"WebViewStorageBridge: Failed to extract data for key '{key}': {ex.Message}");
            return null;
        }
    }

    public async Task<Dictionary<string, string>> GetAllStorageAsync(StorageType storageType)
    {
        try
        {
            var jsStorageName = storageType switch
            {
                StorageType.LocalStorage => "localStorage",
                StorageType.SessionStorage => "sessionStorage",
                StorageType.Cookies => throw new NotSupportedException("Use GetAllCookiesAsync for cookie enumeration"),
                _ => "localStorage"
            };

            var script = $@"
                (() => {{
                    const storage = {jsStorageName};
                    const entries = {{}};
                    for (let i = 0; i < storage.length; i++) {{
                        const key = storage.key(i);
                        entries[key] = storage.getItem(key);
                    }}
                    return JSON.stringify(entries);
                }})()";

            var rawJson = await jsRuntime.InvokeAsync<string>("eval", script);
            return WebViewJsonParser.ParseJson<Dictionary<string, string>>(rawJson) ?? [];
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"WebViewStorageBridge: Failed to get all storage ({storageType}): {ex.Message}");
            return [];
        }
    }

    public async Task StartMonitoringAsync(TimeSpan pollingInterval)
    {
        StopMonitoring();

        _monitoringCts = new CancellationTokenSource();
        var ct = _monitoringCts.Token;

        Debug.WriteLine($"WebViewStorageBridge: Starting monitoring with {pollingInterval.TotalSeconds}s interval for keys: {string.Join(", ", config.MonitoredKeys)}");

        _ = Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(pollingInterval, ct);

                    foreach (var key in config.MonitoredKeys)
                    {
                        var currentData = await ExtractDataAsync(key);

                        if (!Equals(_lastKnownData, currentData))
                        {
                            _lastKnownData = currentData;
                            DataChanged?.Invoke(currentData);
                            Debug.WriteLine($"WebViewStorageBridge: Data changed for key '{key}'");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"WebViewStorageBridge: Monitoring error: {ex.Message}");
                }
            }

            Debug.WriteLine("WebViewStorageBridge: Monitoring stopped");
        }, ct);
    }

    public void StopMonitoring()
    {
        if (_monitoringCts is not null)
        {
            _monitoringCts.Cancel();
            _monitoringCts.Dispose();
            _monitoringCts = null;
        }
    }

    private async Task<string?> ReadStorageValueAsync(string key, StorageType storageType)
    {
        var script = storageType switch
        {
            StorageType.LocalStorage => $"localStorage.getItem('{EscapeJsString(key)}')",
            StorageType.SessionStorage => $"sessionStorage.getItem('{EscapeJsString(key)}')",
            StorageType.Cookies => $"document.cookie.split('; ').find(c => c.startsWith('{EscapeJsString(key)}='))?.split('=').slice(1).join('=')",
            _ => $"localStorage.getItem('{EscapeJsString(key)}')"
        };

        return await jsRuntime.InvokeAsync<string?>("eval", script);
    }

    private static string EscapeJsString(string input) =>
        input.Replace("\\", "\\\\").Replace("'", "\\'");

    public void Dispose()
    {
        if (_disposed) return;
        StopMonitoring();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Configuration for <see cref="WebViewStorageBridge{TData}"/>.
/// Built from <see cref="WebViewBridgeOptions"/> during DI registration.
/// </summary>
public class WebViewStorageBridgeConfig
{
    public string[] MonitoredKeys { get; init; } = [];
    public StorageType DefaultStorageType { get; init; } = StorageType.LocalStorage;
    public TimeSpan PollingInterval { get; init; } = TimeSpan.FromSeconds(3);
}
