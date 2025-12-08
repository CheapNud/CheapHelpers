using CheapHelpers.MediaProcessing.Services;
using CheapHelpers.MediaProcessing.Services.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.MediaProcessing;

/// <summary>
/// Extension methods for registering CheapHelpers.MediaProcessing services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add all media processing services to the service collection
    /// </summary>
    public static IServiceCollection AddMediaProcessing(this IServiceCollection services)
    {
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
    public static IServiceCollection AddMediaProcessing(this IServiceCollection services, string tempBasePath)
    {
        // Core services
        services.AddSingleton<SvpDetectionService>();
        services.AddSingleton<HardwareDetectionService>();
        services.AddSingleton<ExecutableDetectionService>();

        // Utilities
        services.AddSingleton<ProcessManager>();
        services.AddScoped(_ => new TemporaryFileManager(tempBasePath));

        return services;
    }
}
