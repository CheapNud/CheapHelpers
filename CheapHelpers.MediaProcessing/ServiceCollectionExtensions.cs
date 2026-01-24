using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using CheapHelpers.MediaProcessing.Services;
using CheapHelpers.MediaProcessing.Services.Utilities;
using Microsoft.Extensions.DependencyInjection;
#if !WINDOWS
using CheapHelpers.MediaProcessing.Services.Linux;
#endif

namespace CheapHelpers.MediaProcessing;

/// <summary>
/// Extension methods for registering CheapHelpers.MediaProcessing services.
/// Supports both Windows and Linux platforms.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add all media processing services to the service collection.
    /// Automatically selects Windows or Linux implementations based on the runtime platform.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when services is null</exception>
    public static IServiceCollection AddMediaProcessing(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            AddWindowsServices(services);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            AddLinuxServices(services);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            throw new PlatformNotSupportedException(
                "macOS is not currently supported. macOS support is planned for a future release.");
        }
        else
        {
            throw new PlatformNotSupportedException(
                $"Platform {RuntimeInformation.OSDescription} is not supported. " +
                "Only Windows and Linux are currently supported.");
        }

        // Cross-platform utilities
        services.AddSingleton<ProcessManager>();
        services.AddScoped<TemporaryFileManager>();

        return services;
    }

    /// <summary>
    /// Add media processing services with custom temp path.
    /// Automatically selects Windows or Linux implementations based on the runtime platform.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when services is null</exception>
    /// <exception cref="ArgumentException">Thrown when tempBasePath is null or empty</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when tempBasePath directory does not exist</exception>
    public static IServiceCollection AddMediaProcessing(this IServiceCollection services, string tempBasePath)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(tempBasePath);

        if (!Directory.Exists(tempBasePath))
        {
            throw new DirectoryNotFoundException(
                $"The specified temp base path does not exist: {tempBasePath}");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            AddWindowsServices(services);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            AddLinuxServices(services);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            throw new PlatformNotSupportedException(
                "macOS is not currently supported. macOS support is planned for a future release.");
        }
        else
        {
            throw new PlatformNotSupportedException(
                $"Platform {RuntimeInformation.OSDescription} is not supported. " +
                "Only Windows and Linux are currently supported.");
        }

        // Cross-platform utilities
        services.AddSingleton<ProcessManager>();
        services.AddScoped(_ => new TemporaryFileManager(tempBasePath));

        return services;
    }

    /// <summary>
    /// Add Windows-specific media processing services.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when services is null</exception>
    /// <exception cref="PlatformNotSupportedException">Thrown when not running on Windows</exception>
    [SupportedOSPlatform("windows")]
    public static IServiceCollection AddMediaProcessingWindows(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        ThrowIfNotWindows();
        AddWindowsServices(services);

        services.AddSingleton<ProcessManager>();
        services.AddScoped<TemporaryFileManager>();

        return services;
    }

    /// <summary>
    /// Add Linux-specific media processing services.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when services is null</exception>
    /// <exception cref="PlatformNotSupportedException">Thrown when running on Windows</exception>
    [UnsupportedOSPlatform("windows")]
    public static IServiceCollection AddMediaProcessingLinux(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        ThrowIfWindows();
        AddLinuxServices(services);

        services.AddSingleton<ProcessManager>();
        services.AddScoped<TemporaryFileManager>();

        return services;
    }

    [SupportedOSPlatform("windows")]
    private static void AddWindowsServices(IServiceCollection services)
    {
#if WINDOWS
        // Windows-only services (SVP, WMI-based detection)
        services.AddSingleton<SvpDetectionService>();
        services.AddSingleton<HardwareDetectionService>();
        services.AddSingleton<ExecutableDetectionService>();
#else
        throw new PlatformNotSupportedException("Windows services are not available in this build.");
#endif
    }

    [UnsupportedOSPlatform("windows")]
    private static void AddLinuxServices(IServiceCollection services)
    {
#if !WINDOWS
        // Linux services (nvidia-smi, which command)
        services.AddSingleton<LinuxExecutableDetectionService>();
        services.AddSingleton<LinuxHardwareDetectionService>();
#else
        throw new PlatformNotSupportedException("Linux services are not available in this build.");
#endif
    }

    private static void ThrowIfNotWindows()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException(
                "Windows media processing services require Windows operating system.");
        }
    }

    private static void ThrowIfWindows()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException(
                "Linux media processing services require a non-Windows operating system.");
        }
    }
}
