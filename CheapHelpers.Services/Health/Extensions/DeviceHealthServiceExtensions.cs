using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.Services.Health.Extensions;

/// <summary>
/// Extension methods for registering device health monitoring with DI.
/// </summary>
public static class DeviceHealthServiceExtensions
{
    /// <summary>
    /// Adds the device health monitor as a hosted service.
    /// Register individual <see cref="IDeviceHealthCheck"/> implementations before calling this.
    /// </summary>
    public static IServiceCollection AddDeviceHealthMonitoring(
        this IServiceCollection services,
        Action<DeviceHealthMonitorOptions>? configureOptions = null)
    {
        var monitorOptions = new DeviceHealthMonitorOptions();
        configureOptions?.Invoke(monitorOptions);

        services.AddSingleton(monitorOptions);
        services.AddSingleton<DeviceHealthMonitor>();
        services.AddSingleton<IDeviceHealthMonitor>(sp => sp.GetRequiredService<DeviceHealthMonitor>());
        services.AddHostedService(sp => sp.GetRequiredService<DeviceHealthMonitor>());

        return services;
    }

    /// <summary>
    /// Registers an <see cref="IDeviceHealthCheck"/> implementation.
    /// </summary>
    public static IServiceCollection AddDeviceHealthCheck<TCheck>(this IServiceCollection services)
        where TCheck : class, IDeviceHealthCheck
    {
        services.AddSingleton<IDeviceHealthCheck, TCheck>();
        return services;
    }
}
