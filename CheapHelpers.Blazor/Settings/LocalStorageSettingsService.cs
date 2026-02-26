using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace CheapHelpers.Settings;

/// <summary>
/// Browser-based settings service using plain localStorage via <see cref="IJSRuntime"/>.
/// No encryption — data is stored as plain JSON strings.
/// Works in both Blazor Server and Blazor WebAssembly.
/// </summary>
public class LocalStorageSettingsService : ISettingsService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<LocalStorageSettingsService>? _logger;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public event Action? SettingsChanged;

    public LocalStorageSettingsService(
        IJSRuntime jsRuntime,
        ILogger<LocalStorageSettingsService>? logger = null)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    // ───────────────────────────── Key-value API ─────────────────────────────

    public async Task<T?> GetAsync<T>(string key, T? defaultValue = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
            if (json is not null)
            {
                return JsonSerializer.Deserialize<T>(json, SerializerOptions);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to read setting '{Key}' from localStorage, returning default", key);
        }

        return defaultValue;
    }

    public async Task SetAsync<T>(string key, T settingValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(settingValue);

        var json = JsonSerializer.Serialize(settingValue, SerializerOptions);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, json);

        _logger?.LogDebug("Setting '{Key}' saved to localStorage", key);
        SettingsChanged?.Invoke();
    }

    public async Task<bool> DeleteAsync(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var existing = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
            if (existing is null)
                return false;

            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
            _logger?.LogDebug("Setting '{Key}' deleted from localStorage", key);
            SettingsChanged?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to delete setting '{Key}' from localStorage", key);
            return false;
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var existing = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
            return existing is not null;
        }
        catch
        {
            return false;
        }
    }

    // ───────────────────────────── Typed section API ─────────────────────────

    public async Task<T> GetSectionAsync<T>(T? defaultValue = default) where T : class, new()
    {
        var sectionKey = typeof(T).Name;
        var fetched = await GetAsync<T>(sectionKey);
        return fetched ?? defaultValue ?? new T();
    }

    public Task SetSectionAsync<T>(T settingValue) where T : class
    {
        ArgumentNullException.ThrowIfNull(settingValue);
        var sectionKey = typeof(T).Name;
        return SetAsync(sectionKey, settingValue);
    }

    public async Task UpdateSectionAsync<T>(Action<T> updateAction) where T : class, new()
    {
        ArgumentNullException.ThrowIfNull(updateAction);

        var section = await GetSectionAsync<T>();
        updateAction(section);
        await SetSectionAsync(section);
    }

    // ───────────────────────────── Utility ────────────────────────────────────

    /// <summary>
    /// No-op — localStorage saves immediately on each SetAsync call.
    /// </summary>
    public Task SaveAsync() => Task.CompletedTask;

    /// <summary>
    /// No-op — localStorage reads directly on each GetAsync call.
    /// </summary>
    public Task ReloadAsync() => Task.CompletedTask;
}
