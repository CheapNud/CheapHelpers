using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.Settings;

/// <summary>
/// Extension methods for registering browser-based <see cref="ISettingsService"/> implementations.
/// </summary>
public static class BrowserSettingsServiceExtensions
{
    /// <summary>
    /// Registers <see cref="ProtectedBrowserSettingsService"/> as the <see cref="ISettingsService"/> singleton.
    /// Uses ASP.NET Core's ProtectedLocalStorage (encrypted, Blazor Server only).
    /// </summary>
    public static IServiceCollection AddProtectedBrowserSettingsService(this IServiceCollection services)
    {
        services.AddSingleton<ISettingsService, ProtectedBrowserSettingsService>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="LocalStorageSettingsService"/> as the <see cref="ISettingsService"/> singleton.
    /// Uses plain localStorage via IJSRuntime (no encryption, works in Server and WebAssembly).
    /// </summary>
    public static IServiceCollection AddLocalStorageSettingsService(this IServiceCollection services)
    {
        services.AddSingleton<ISettingsService, LocalStorageSettingsService>();
        return services;
    }
}
