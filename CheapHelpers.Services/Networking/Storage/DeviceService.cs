using CheapHelpers.Networking.Core;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Networking.Storage;

/// <summary>
/// Service for managing known network devices with persistence
/// </summary>
public class DeviceService(ILogger<DeviceService> logger, IDeviceStorage storage) : IDeviceService
{
    private readonly List<NetworkDevice> _myDevices = [];
    private string _lastConnectedDeviceIp = string.Empty;
    private bool _devicesLoaded;

    public event Action<string>? StatusChanged;
    public event Action<NetworkDevice>? DeviceAdded;
    public event Action<NetworkDevice>? DeviceRemoved;

    public async Task<List<NetworkDevice>> GetDevicesAsync()
    {
        StatusChanged?.Invoke("Loading saved devices...");

        if (!_devicesLoaded)
        {
            await LoadPersistedDevicesAsync();
            await LoadAppSettingsAsync();
            _devicesLoaded = true;
        }

        StatusChanged?.Invoke($"Loaded {_myDevices.Count} saved devices");
        return [.. _myDevices];
    }

    public async Task<bool> AddDeviceAsync(NetworkDevice device)
    {
        if (device == null)
        {
            logger.LogWarning("Cannot add null device");
            return false;
        }

        var existingDevice = _myDevices.FirstOrDefault(d => d.IPv4Address == device.IPv4Address);
        if (existingDevice != null)
        {
            logger.LogWarning("Device with IP {IpAddress} already exists in known devices", device.IPv4Address);
            return false;
        }

        _myDevices.Add(device);

        await SavePersistedDevicesAsync();

        DeviceAdded?.Invoke(device);
        StatusChanged?.Invoke($"Device {device.Name} added to known devices");

        return true;
    }

    public async Task<bool> RemoveDeviceAsync(string ipAddress)
    {
        var removedDevice = _myDevices.FirstOrDefault(d => d.IPv4Address == ipAddress);
        if (removedDevice == null)
        {
            logger.LogWarning("Device with IP {IpAddress} not found in known devices", ipAddress);
            return false;
        }

        _myDevices.Remove(removedDevice);

        if (_lastConnectedDeviceIp == ipAddress)
        {
            _lastConnectedDeviceIp = string.Empty;
            await SaveAppSettingsAsync();
        }

        await SavePersistedDevicesAsync();

        DeviceRemoved?.Invoke(removedDevice);
        StatusChanged?.Invoke($"Device {removedDevice.Name} removed from known devices");

        return true;
    }

    public async Task<bool> UpdateDeviceAsync(NetworkDevice updatedDevice)
    {
        var existingDevice = _myDevices.FirstOrDefault(d => d.IPv4Address == updatedDevice.IPv4Address);
        if (existingDevice == null)
        {
            logger.LogWarning("Device with IP {IpAddress} not found for update", updatedDevice.IPv4Address);
            return false;
        }

        existingDevice.IsOnline = updatedDevice.IsOnline;
        existingDevice.LastSeen = updatedDevice.LastSeen;
        existingDevice.ResponseTime = updatedDevice.ResponseTime;
        existingDevice.Name = updatedDevice.Name;
        existingDevice.Type = updatedDevice.Type;
        existingDevice.MacAddress = updatedDevice.MacAddress;

        logger.LogDebug("Updated device in known devices: {Name} ({IpAddress})", existingDevice.Name, existingDevice.IPv4Address);

        await SavePersistedDevicesAsync();

        StatusChanged?.Invoke($"Device {existingDevice.Name} updated");

        return true;
    }

    public Task<NetworkDevice?> GetDeviceByIpAsync(string ipAddress)
    {
        return Task.FromResult(_myDevices.FirstOrDefault(d => d.IPv4Address == ipAddress));
    }

    public async Task<List<string>> GetKnownDeviceIpsAsync()
    {
        if (!_devicesLoaded)
        {
            await LoadPersistedDevicesAsync();
            _devicesLoaded = true;
        }

        return [.. _myDevices.Select(d => d.IPv4Address)];
    }

    public async Task SetLastConnectedDeviceAsync(string ipAddress)
    {
        _lastConnectedDeviceIp = ipAddress;
        await SaveAppSettingsAsync();
        logger.LogDebug("Set last connected device to {IpAddress}", ipAddress);
    }

    public async Task<string> GetLastConnectedDeviceAsync()
    {
        if (string.IsNullOrEmpty(_lastConnectedDeviceIp))
        {
            await LoadAppSettingsAsync();
        }

        logger.LogDebug("Retrieved last connected device: {IpAddress}", _lastConnectedDeviceIp);
        return _lastConnectedDeviceIp;
    }

    private async Task LoadPersistedDevicesAsync()
    {
        var loadedDevices = await storage.LoadDevicesAsync();
        _myDevices.Clear();
        _myDevices.AddRange(loadedDevices);

        logger.LogInformation("Loaded {Count} persisted devices", loadedDevices.Count);
    }

    private async Task SavePersistedDevicesAsync()
    {
        await storage.SaveDevicesAsync(_myDevices);
        logger.LogDebug("Saved {Count} devices", _myDevices.Count);
    }

    private async Task LoadAppSettingsAsync()
    {
        var loadedSettings = await storage.LoadSettingsAsync();
        if (loadedSettings.TryGetValue("LastConnectedDeviceIp", out var lastIp))
        {
            _lastConnectedDeviceIp = lastIp;
        }
    }

    private async Task SaveAppSettingsAsync()
    {
        var appSettings = new Dictionary<string, string>
        {
            ["LastConnectedDeviceIp"] = _lastConnectedDeviceIp
        };

        await storage.SaveSettingsAsync(appSettings);
    }
}
