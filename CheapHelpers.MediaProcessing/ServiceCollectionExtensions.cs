using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using CheapHelpers.MediaProcessing.Services;
using CheapHelpers.MediaProcessing.Services.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.MediaProcessing;

/// <summary>
/// Extension methods for registering CheapHelpers.MediaProcessing services
/// </summary>
/// <remarks>
/// PLATFORM REQUIREMENT: All services in this library require Windows operating system.
/// Attempting to register services on non-Windows platforms will throw <see cref="PlatformNotSupportedException"/>.
/// </remarks>
[SupportedOSPlatform("windows")]
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add all media processing services to the service collection
    /// </summary>
    /// <exception cref="PlatformNotSupportedException">Thrown when running on non-Windows platforms</exception>
    public static IServiceCollection AddMediaProcessing(this IServiceCollection services)
    {
        ThrowIfNotWindows();

        // Core services
        services.AddSingleton<SvpDetectionService>();
        services.AddSingleton<HardwareDetectionService>();
        services.AddSingleton<ExecutableDetectionService>();

        // Utilities
        services.AddSingleton<ProcessManager>();
        services.AddScoped<TemporaryFileManager>();

        return services;
    }

    /// <summary>
    /// Add media processing services with custom temp path
    /// </summary>
    /// <exception cref="PlatformNotSupportedException">Thrown when running on non-Windows platforms</exception>
    public static IServiceCollection AddMediaProcessing(this IServiceCollection services, string tempBasePath)
    {
        ThrowIfNotWindows();

        // Core services
        services.AddSingleton<SvpDetectionService>();
        services.AddSingleton<HardwareDetectionService>();
        services.AddSingleton<ExecutableDetectionService>();

        // Utilities
        services.AddSingleton<ProcessManager>();
        services.AddScoped(_ => new TemporaryFileManager(tempBasePath));

        return services;
    }

    /// <summary>
    /// Throws PlatformNotSupportedException if not running on Windows
    /// </summary>
    private static void ThrowIfNotWindows()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException(
                "CheapHelpers.MediaProcessing requires Windows operating system. " +
                "This library uses Windows Management Instrumentation (WMI), Windows-specific paths, " +
                "and the 'where' command which are not available on other platforms.");
        }
    }
}
