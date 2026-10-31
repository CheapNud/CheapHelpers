using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace CheapHelpers.Services.Vision;

/// <summary>
/// Registers <see cref="AzureVisionService"/> as <see cref="IVisionService"/> with options that are validated when the host starts.
/// </summary>
public static class VisionServiceExtensions
{
    private const string MissingSettingsMessage = "AzureVision:Endpoint and AzureVision:ApiKey are required";

    /// <summary>
    /// Registers the Azure vision service with options bound from the <see cref="VisionServiceOptions.SectionName"/> section.
    /// </summary>
    public static IServiceCollection AddAzureVision(this IServiceCollection services, IConfiguration configuration)
    {
        AddValidatedOptions(services).Bind(configuration.GetSection(VisionServiceOptions.SectionName));
        return AddService(services);
    }

    /// <summary>
    /// Registers the Azure vision service with options set by <paramref name="configure"/>.
    /// </summary>
    public static IServiceCollection AddAzureVision(this IServiceCollection services, Action<VisionServiceOptions> configure)
    {
        AddValidatedOptions(services).Configure(configure);
        return AddService(services);
    }

    private static OptionsBuilder<VisionServiceOptions> AddValidatedOptions(IServiceCollection services)
    {
        var builder = services.AddOptions<VisionServiceOptions>();
        builder
            .Validate(o => !string.IsNullOrWhiteSpace(o.Endpoint) && !string.IsNullOrWhiteSpace(o.ApiKey), MissingSettingsMessage)
            .ValidateOnStart();
        return builder;
    }

    // Singleton because the underlying ImageAnalysisClient is thread-safe.
    private static IServiceCollection AddService(IServiceCollection services)
    {
        services.TryAddSingleton<IVisionService, AzureVisionService>();
        return services;
    }
}
