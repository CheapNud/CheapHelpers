namespace CheapHelpers.Settings;

/// <summary>
/// JSON-based settings persistence service with key-value and typed section APIs.
/// Settings are stored in a single JSON file (file backend) or browser storage (browser backends).
/// </summary>
public interface ISettingsService
{
    // Key-value API

    /// <summary>
    /// Get a value by key, returning the default if not found
    /// </summary>
    Task<T?> GetAsync<T>(string key, T? defaultValue = default);

    /// <summary>
    /// Set a value by key (auto-saves when AutoSave is enabled)
    /// </summary>
    Task SetAsync<T>(string key, T settingValue);

    /// <summary>
    /// Delete a key from settings (auto-saves when AutoSave is enabled)
    /// </summary>
    Task<bool> DeleteAsync(string key);

    /// <summary>
    /// Check if a key exists in settings
    /// </summary>
    Task<bool> ExistsAsync(string key);

    // Typed section API (key = typeof(T).Name)

    /// <summary>
    /// Get a typed settings section. The key is typeof(T).Name.
    /// Returns the stored section or a new instance with defaults.
    /// </summary>
    Task<T> GetSectionAsync<T>(T? defaultValue = default) where T : class, new();

    /// <summary>
    /// Replace a typed settings section entirely (auto-saves when AutoSave is enabled)
    /// </summary>
    Task SetSectionAsync<T>(T settingValue) where T : class;

    /// <summary>
    /// Read-modify-write a typed settings section (auto-saves when AutoSave is enabled)
    /// </summary>
    Task UpdateSectionAsync<T>(Action<T> updateAction) where T : class, new();

    // Utility

    /// <summary>
    /// Explicitly save all settings to the backing store
    /// </summary>
    Task SaveAsync();

    /// <summary>
    /// Reload settings from the backing store, discarding in-memory changes
    /// </summary>
    Task ReloadAsync();

    /// <summary>
    /// Fired after settings are persisted to the backing store
    /// </summary>
    event Action? SettingsChanged;
}
