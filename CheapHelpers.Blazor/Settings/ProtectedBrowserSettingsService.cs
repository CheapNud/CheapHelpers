using System.Text.Json;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Settings;

/// <summary>
/// Browser-based settings service using ASP.NET Core's <see cref="ProtectedLocalStorage"/>.
/// Data is encrypted and stored in the browser's localStorage. Blazor Server only.
/// Each key-value pair is stored independently (no lazy-load of entire file).
/// </summary>
public class ProtectedBrowserSettingsService : ISettingsService
{
    private readonly ProtectedLocalStorage _storage;
    private readonly ILogger<ProtectedBrowserSettingsService>? _logger;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public event Action? SettingsChanged;

    public ProtectedBrowserSettingsService(
        ProtectedLocalStorage storage,
        ILogger<ProtectedBrowserSettingsService>? logger = null)
    {
        _storage = storage;
        _logger = logger;
    }

    // ───────────────────────────── Key-value API ─────────────────────────────

    public async Task<T?> GetAsync<T>(string key, T? defaultValue = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var storedResult = await _storage.GetAsync<string>(key);
            if (storedResult.Success && storedResult.Value is not null)
            {
                return JsonSerializer.Deserialize<T>(storedResult.Value, SerializerOptions);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to read setting '{Key}' from protected storage, returning default", key);
        }

        return defaultValue;
    }

    public async Task SetAsync<T>(string key, T settingValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(settingValue);

        var json = JsonSerializer.Serialize(settingValue, SerializerOptions);
        await _storage.SetAsync(key, json);

        _logger?.LogDebug("Setting '{Key}' saved to protected storage", key);
        SettingsChanged?.Invoke();
    }

    public async Task<bool> DeleteAsync(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var existsResult = await _storage.GetAsync<string>(key);
            if (!existsResult.Success)
                return false;

            await _storage.DeleteAsync(key);
            _logger?.LogDebug("Setting '{Key}' deleted from protected storage", key);
            SettingsChanged?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to delete setting '{Key}' from protected storage", key);
            return false;
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var storedResult = await _storage.GetAsync<string>(key);
            return storedResult.Success;
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
    /// No-op — browser storage saves immediately on each SetAsync call.
    /// </summary>
    public Task SaveAsync() => Task.CompletedTask;

    /// <summary>
    /// No-op — browser storage reads directly on each GetAsync call.
    /// </summary>
    public Task ReloadAsync() => Task.CompletedTask;
}
