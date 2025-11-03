using CheapHelpers.Networking.Core;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CheapHelpers.Networking.Storage;

/// <summary>
/// JSON file-based storage implementation for network devices
/// </summary>
public class JsonFileStorage : IDeviceStorage
{
    private readonly ILogger<JsonFileStorage> _logger;
    private readonly string _deviceDataPath;
    private readonly string _settingsPath;

    public JsonFileStorage(ILogger<JsonFileStorage> logger, string? applicationName = null)
    {
        _logger = logger;

        var appName = applicationName ?? "CheapHelpers.Networking";
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, appName);
        Directory.CreateDirectory(appFolder);

        _deviceDataPath = Path.Combine(appFolder, "known_devices.json");
        _settingsPath = Path.Combine(appFolder, "app_settings.json");

        _logger.LogDebug("Device data path: {DeviceDataPath}", _deviceDataPath);
        _logger.LogDebug("Settings path: {SettingsPath}", _settingsPath);
    }

    public async Task<List<NetworkDevice>> LoadDevicesAsync()
    {
        try
        {
            if (!File.Exists(_deviceDataPath))
            {
                _logger.LogDebug("No persisted devices file found");
                return [];
            }

            var jsonData = await File.ReadAllTextAsync(_deviceDataPath);
            if (string.IsNullOrWhiteSpace(jsonData))
            {
                _logger.LogDebug("Empty persisted devices file");
                return [];
            }

            var loadedDevices = JsonSerializer.Deserialize<List<NetworkDevice>>(jsonData, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (loadedDevices != null)
            {
                _logger.LogInformation("Loaded {Count} persisted devices from {Path}", loadedDevices.Count, _deviceDataPath);
                return loadedDevices;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading persisted devices");
        }

        return [];
    }

    public async Task SaveDevicesAsync(List<NetworkDevice> devices)
    {
        try
        {
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var jsonData = JsonSerializer.Serialize(devices, jsonOptions);
            await File.WriteAllTextAsync(_deviceDataPath, jsonData);

            _logger.LogDebug("Saved {Count} devices to {Path}", devices.Count, _deviceDataPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving persisted devices");
        }
    }

    public async Task<Dictionary<string, string>> LoadSettingsAsync()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                _logger.LogDebug("No app settings file found");
                return [];
            }

            var jsonData = await File.ReadAllTextAsync(_settingsPath);
            if (string.IsNullOrWhiteSpace(jsonData))
            {
                _logger.LogDebug("Empty app settings file");
                return [];
            }

            var loadedSettings = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonData, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return loadedSettings ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading app settings");
            return [];
        }
    }

    public async Task SaveSettingsAsync(Dictionary<string, string> settings)
    {
        try
        {
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var jsonData = JsonSerializer.Serialize(settings, jsonOptions);
            await File.WriteAllTextAsync(_settingsPath, jsonData);

            _logger.LogDebug("Saved app settings to {Path}", _settingsPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving app settings");
        }
    }
}
