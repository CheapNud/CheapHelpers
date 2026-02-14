namespace CheapHelpers.Settings;

/// <summary>
/// Interface for a JSON-based settings persistence service.
/// </summary>
/// <typeparam name="T">The settings type. Must be a reference type with a parameterless constructor.</typeparam>
public interface IJsonSettingsService<T> where T : class, new()
{
    /// <summary>
    /// The current in-memory settings instance.
    /// Returns default settings if <see cref="LoadAsync"/> has not been called yet.
    /// </summary>
    T Settings { get; }

    /// <summary>
    /// Raised after settings are saved to disk.
    /// </summary>
    event Action? SettingsChanged;

    /// <summary>
    /// Loads settings from the JSON file on disk.
    /// Returns cached settings on subsequent calls without re-reading the file.
    /// If no file exists, creates and persists default settings.
    /// </summary>
    Task<T> LoadAsync();

    /// <summary>
    /// Persists the current <see cref="Settings"/> instance to disk.
    /// </summary>
    Task SaveAsync();

    /// <summary>
    /// Replaces the current settings with <paramref name="settings"/> and persists to disk.
    /// </summary>
    /// <param name="settings">The settings instance to save.</param>
    Task SaveAsync(T settings);

    /// <summary>
    /// Resets settings to defaults (via <see cref="JsonSettingsService{T}.CreateDefaultSettings"/>)
    /// and persists the result to disk.
    /// </summary>
    Task ResetToDefaultsAsync();

    /// <summary>
    /// Returns the full path to the settings JSON file.
    /// </summary>
    string GetSettingsFilePath();
}
