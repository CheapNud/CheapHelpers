using CheapHelpers.Networking.Core;
using CheapHelpers.Networking.Detection;
using CheapHelpers.Networking.MacResolution;
using CheapHelpers.Networking.Storage;
using CheapHelpers.Networking.Subnet;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.InteropServices;

namespace CheapHelpers.Networking.Extensions;

/// <summary>
/// Extension methods for configuring network scanning services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds network scanning services with default configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureScanner">Optional configuration for scanner options</param>
    /// <param name="configurePorts">Optional configuration for port detection options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddNetworkScanning(
        this IServiceCollection services,
        Action<NetworkScannerOptions>? configureScanner = null,
        Action<PortDetectionOptions>? configurePorts = null)
    {
        if (configureScanner != null)
        {
            services.Configure(configureScanner);
        }
        else
        {
            services.Configure<NetworkScannerOptions>(_ => { });
        }

        if (configurePorts != null)
        {
            services.Configure(configurePorts);
        }
        else
        {
            services.Configure<PortDetectionOptions>(_ => { });
        }

        services.AddSingleton<INetworkScanner, NetworkScanner>();
        services.AddSingleton<ISubnetProvider, LocalSubnetProvider>();

        AddPlatformSpecificMacResolver(services);

        return services;
    }

    /// <summary>
    /// Adds default device type detectors (HTTP, SSH, Windows, Service Endpoints)
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddDefaultDetectors(this IServiceCollection services)
    {
        services.AddSingleton<IDeviceTypeDetector, ServiceEndpointDetector>();
        services.AddSingleton<IDeviceTypeDetector, HttpDetector>();
        services.AddSingleton<IDeviceTypeDetector, SshDetector>();
        services.AddSingleton<IDeviceTypeDetector, WindowsServicesDetector>();

        return services;
    }

    /// <summary>
    /// Adds enhanced device discovery detectors (UPnP/SSDP and mDNS/Zeroconf)
    /// These provide significantly better device discovery for IoT devices, smart home devices,
    /// printers, media servers, and other UPnP/mDNS-enabled devices.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddEnhancedDetectors(this IServiceCollection services)
    {
        services.AddSingleton<IDeviceTypeDetector, UpnpDetector>();
        services.AddSingleton<IDeviceTypeDetector, MdnsDetector>();

        return services;
    }

    /// <summary>
    /// Adds all available detectors (default + enhanced)
    /// Includes: UPnP/SSDP, mDNS/Zeroconf, HTTP, SSH, Windows Services, and Service Endpoints
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAllDetectors(this IServiceCollection services)
    {
        return services
            .AddDefaultDetectors()
            .AddEnhancedDetectors();
    }

    /// <summary>
    /// Adds UPnP/SSDP device detector
    /// Discovers devices like smart TVs, media servers, routers, printers, and IoT devices
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddUpnpDetector(this IServiceCollection services)
    {
        services.AddSingleton<IDeviceTypeDetector, UpnpDetector>();
        return services;
    }

    /// <summary>
    /// Adds mDNS/Zeroconf/Bonjour device detector
    /// Discovers devices like printers, Apple devices, IoT devices, and network services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddMdnsDetector(this IServiceCollection services)
    {
        services.AddSingleton<IDeviceTypeDetector, MdnsDetector>();
        return services;
    }

    /// <summary>
    /// Adds a custom device type detector
    /// </summary>
    /// <typeparam name="TDetector">The detector type</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCustomDetector<TDetector>(this IServiceCollection services)
        where TDetector : class, IDeviceTypeDetector
    {
        services.AddSingleton<IDeviceTypeDetector, TDetector>();
        return services;
    }

    /// <summary>
    /// Adds JSON file storage for device persistence
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="applicationName">Optional application name for storage folder (defaults to "CheapHelpers.Networking")</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddJsonStorage(this IServiceCollection services, string? applicationName = null)
    {
        services.AddSingleton<IDeviceStorage>(sp =>
        {
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<JsonFileStorage>>();
            return new JsonFileStorage(logger, applicationName);
        });

        services.AddSingleton<IDeviceService, DeviceService>();

        return services;
    }

    /// <summary>
    /// Adds a custom storage implementation
    /// </summary>
    /// <typeparam name="TStorage">The storage implementation type</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCustomStorage<TStorage>(this IServiceCollection services)
        where TStorage : class, IDeviceStorage
    {
        services.AddSingleton<IDeviceStorage, TStorage>();
        services.AddSingleton<IDeviceService, DeviceService>();

        return services;
    }

    /// <summary>
    /// Adds a custom subnet provider
    /// </summary>
    /// <typeparam name="TProvider">The subnet provider type</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCustomSubnetProvider<TProvider>(this IServiceCollection services)
        where TProvider : class, ISubnetProvider
    {
        services.AddSingleton<ISubnetProvider, TProvider>();
        return services;
    }

    /// <summary>
    /// Adds a custom MAC address resolver
    /// </summary>
    /// <typeparam name="TResolver">The MAC resolver type</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCustomMacResolver<TResolver>(this IServiceCollection services)
        where TResolver : class, IMacAddressResolver
    {
        services.AddSingleton<IMacAddressResolver, TResolver>();
        return services;
    }

    private static void AddPlatformSpecificMacResolver(IServiceCollection services)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            services.AddSingleton<IMacAddressResolver, WindowsArpResolver>();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            services.AddSingleton<IMacAddressResolver, LinuxArpResolver>();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            services.AddSingleton<IMacAddressResolver, MacOSArpResolver>();
        }
        else
        {
            services.AddSingleton<IMacAddressResolver, WindowsArpResolver>();
        }
    }
}
