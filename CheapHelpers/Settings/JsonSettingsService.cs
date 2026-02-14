using System.Diagnostics;
using System.Text.Json;

namespace CheapHelpers.Settings;

/// <summary>
/// Base class for JSON-based settings persistence.
/// Stores settings as a JSON file in an application-specific subfolder under the user's AppData directory.
/// Thread-safe via <see cref="SemaphoreSlim"/>. Override <see cref="CreateDefaultSettings"/>
/// to provide application-specific defaults (e.g., auto-detected paths).
/// </summary>
/// <typeparam name="T">The settings type. Must be a reference type with a parameterless constructor.</typeparam>
public class JsonSettingsService<T> : IJsonSettingsService<T> where T : class, new()
{
    private readonly string _settingsFilePath;
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions;
    private T? _cachedSettings;
    private bool _isLoaded;

    /// <inheritdoc />
    public T Settings => _cachedSettings ?? new T();

    /// <inheritdoc />
    public event Action? SettingsChanged;

    /// <summary>
    /// Initializes the settings service, creating the application folder if it doesn't exist.
    /// </summary>
    /// <param name="appName">Application name used as the subfolder under AppData (e.g., "CheapUpscaler").</param>
    /// <param name="folder">
    /// The special folder to use as the root. Default is <see cref="Environment.SpecialFolder.LocalApplicationData"/>.
    /// </param>
    /// <param name="fileName">The settings file name. Default is "settings.json".</param>
    /// <param name="jsonOptions">
    /// Custom JSON serializer options. Default uses <see cref="JsonSerializerOptions.WriteIndented"/> = true.
    /// </param>
    protected JsonSettingsService(
        string appName,
        Environment.SpecialFolder folder = Environment.SpecialFolder.LocalApplicationData,
        string fileName = "settings.json",
        JsonSerializerOptions? jsonOptions = null)
    {
        var appDataPath = Environment.GetFolderPath(folder);
        var appFolder = Path.Combine(appDataPath, appName);
        Directory.CreateDirectory(appFolder);
        _settingsFilePath = Path.Combine(appFolder, fileName);
        _jsonOptions = jsonOptions ?? new JsonSerializerOptions { WriteIndented = true };

        Debug.WriteLine($"Settings file path: {_settingsFilePath}");
    }

    /// <summary>
    /// Creates a default settings instance. Override this to provide
    /// application-specific defaults such as auto-detected executable paths.
    /// </summary>
    protected virtual T CreateDefaultSettings() => new();

    /// <inheritdoc />
    public async Task<T> LoadAsync()
    {
        if (_isLoaded)
            return Settings;

        await _fileLock.WaitAsync();
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = await File.ReadAllTextAsync(_settingsFilePath);
                var loaded = JsonSerializer.Deserialize<T>(json, _jsonOptions);

                if (loaded != null)
                {
                    _cachedSettings = loaded;
                    _isLoaded = true;
                    Debug.WriteLine("Settings loaded from file");
                    return loaded;
                }
            }

            Debug.WriteLine("No settings file found, creating defaults");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading settings: {ex.Message}");
        }
        finally
        {
            _fileLock.Release();
        }

        // Create and persist defaults
        _cachedSettings = CreateDefaultSettings();
        await SaveInternalAsync(_cachedSettings);
        _isLoaded = true;
        return _cachedSettings;
    }

    /// <inheritdoc />
    public Task SaveAsync() => SaveInternalAsync(Settings);

    /// <inheritdoc />
    public Task SaveAsync(T settings)
    {
        _cachedSettings = settings;
        return SaveInternalAsync(settings);
    }

    /// <inheritdoc />
    public async Task ResetToDefaultsAsync()
    {
        _cachedSettings = CreateDefaultSettings();
        await SaveInternalAsync(_cachedSettings);
        Debug.WriteLine("Settings reset to defaults");
    }

    /// <inheritdoc />
    public string GetSettingsFilePath() => _settingsFilePath;

    private async Task SaveInternalAsync(T settings)
    {
        await _fileLock.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            await File.WriteAllTextAsync(_settingsFilePath, json);
            _cachedSettings = settings;
            _isLoaded = true;
            Debug.WriteLine("Settings saved");
            SettingsChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving settings: {ex.Message}");
            throw;
        }
        finally
        {
            _fileLock.Release();
        }
    }
}
