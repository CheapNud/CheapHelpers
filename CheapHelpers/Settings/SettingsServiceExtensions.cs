using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.Settings;

/// <summary>
/// Extension methods for registering <see cref="FileSettingsService"/> in the DI container.
/// </summary>
public static class SettingsServiceExtensions
{
    /// <summary>
    /// Registers <see cref="FileSettingsService"/> as the <see cref="ISettingsService"/> singleton.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for <see cref="SettingsServiceOptions"/>.</param>
    public static IServiceCollection AddFileSettingsService(
        this IServiceCollection services,
        Action<SettingsServiceOptions>? configure = null)
    {
        var settingsOptions = new SettingsServiceOptions();
        configure?.Invoke(settingsOptions);

        services.AddSingleton(settingsOptions);
        services.AddSingleton<ISettingsService, FileSettingsService>();

        return services;
    }
}
