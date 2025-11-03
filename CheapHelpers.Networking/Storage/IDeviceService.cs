using CheapHelpers.Networking.Core;

namespace CheapHelpers.Networking.Storage;

/// <summary>
/// Interface for managing known network devices
/// </summary>
public interface IDeviceService
{
    /// <summary>
    /// Gets all known devices
    /// </summary>
    /// <returns>List of known devices</returns>
    Task<List<NetworkDevice>> GetDevicesAsync();

    /// <summary>
    /// Adds a device to the known devices list
    /// </summary>
    /// <param name="device">Device to add</param>
    /// <returns>True if added successfully, false if already exists</returns>
    Task<bool> AddDeviceAsync(NetworkDevice device);

    /// <summary>
    /// Removes a device from the known devices list
    /// </summary>
    /// <param name="ipAddress">IP address of device to remove</param>
    /// <returns>True if removed successfully, false if not found</returns>
    Task<bool> RemoveDeviceAsync(string ipAddress);

    /// <summary>
    /// Updates an existing device with new information
    /// </summary>
    /// <param name="updatedDevice">Updated device information</param>
    /// <returns>True if updated successfully, false if not found</returns>
    Task<bool> UpdateDeviceAsync(NetworkDevice updatedDevice);

    /// <summary>
    /// Gets a specific device by IP address
    /// </summary>
    /// <param name="ipAddress">IP address to search for</param>
    /// <returns>The device if found, null otherwise</returns>
    Task<NetworkDevice?> GetDeviceByIpAsync(string ipAddress);

    /// <summary>
    /// Gets list of all known device IP addresses
    /// </summary>
    /// <returns>List of IP addresses</returns>
    Task<List<string>> GetKnownDeviceIpsAsync();

    /// <summary>
    /// Sets the last connected device IP address
    /// </summary>
    /// <param name="ipAddress">IP address to store</param>
    Task SetLastConnectedDeviceAsync(string ipAddress);

    /// <summary>
    /// Gets the last connected device IP address
    /// </summary>
    /// <returns>Last connected IP address, or empty string if none</returns>
    Task<string> GetLastConnectedDeviceAsync();

    /// <summary>
    /// Event fired when status changes
    /// </summary>
    event Action<string>? StatusChanged;

    /// <summary>
    /// Event fired when a device is added
    /// </summary>
    event Action<NetworkDevice>? DeviceAdded;

    /// <summary>
    /// Event fired when a device is removed
    /// </summary>
    event Action<NetworkDevice>? DeviceRemoved;
}
