using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.Settings;

/// <summary>
/// Extension methods for registering browser-based <see cref="ISettingsService"/> implementations.
/// </summary>
public static class BrowserSettingsServiceExtensions
{
    /// <summary>
    /// Registers <see cref="ProtectedBrowserSettingsService"/> as the scoped <see cref="ISettingsService"/>.
    /// Uses ASP.NET Core's ProtectedLocalStorage (encrypted, Blazor Server only).
    /// Scoped because ProtectedLocalStorage is scoped to the circuit.
    /// </summary>
    public static IServiceCollection AddProtectedBrowserSettingsService(this IServiceCollection services)
    {
        services.AddScoped<ISettingsService, ProtectedBrowserSettingsService>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="LocalStorageSettingsService"/> as the scoped <see cref="ISettingsService"/>.
    /// Uses plain localStorage via IJSRuntime (no encryption, works in Server and WebAssembly).
    /// Scoped because IJSRuntime is scoped to the circuit.
    /// </summary>
    public static IServiceCollection AddLocalStorageSettingsService(this IServiceCollection services)
    {
        services.AddScoped<ISettingsService, LocalStorageSettingsService>();
        return services;
    }
}
