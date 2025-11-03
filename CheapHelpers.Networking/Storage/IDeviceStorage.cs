using CheapHelpers.Networking.Core;

namespace CheapHelpers.Networking.Storage;

/// <summary>
/// Interface for persisting and loading network devices
/// </summary>
public interface IDeviceStorage
{
    /// <summary>
    /// Loads devices from storage
    /// </summary>
    /// <returns>List of saved devices</returns>
    Task<List<NetworkDevice>> LoadDevicesAsync();

    /// <summary>
    /// Saves devices to storage
    /// </summary>
    /// <param name="devices">Devices to save</param>
    Task SaveDevicesAsync(List<NetworkDevice> devices);

    /// <summary>
    /// Loads application settings from storage
    /// </summary>
    /// <returns>Dictionary of settings key-value pairs</returns>
    Task<Dictionary<string, string>> LoadSettingsAsync();

    /// <summary>
    /// Saves application settings to storage
    /// </summary>
    /// <param name="settings">Settings to save</param>
    Task SaveSettingsAsync(Dictionary<string, string> settings);
}
