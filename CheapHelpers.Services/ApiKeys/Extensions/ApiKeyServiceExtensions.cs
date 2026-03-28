using CheapHelpers.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.Services.ApiKeys.Extensions;

/// <summary>
/// Extension methods for registering API key services.
/// </summary>
public static class ApiKeyServiceExtensions
{
    /// <summary>
    /// Adds API key management services to the service collection.
    /// Registers <see cref="IApiKeyService"/> for key generation, validation, revocation, and rotation.
    /// </summary>
    /// <typeparam name="TUser">The Identity user type used by the application.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional configuration for <see cref="ApiKeyOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCheapApiKeys<TUser>(
        this IServiceCollection services,
        Action<ApiKeyOptions>? configureOptions = null)
        where TUser : IdentityUser
    {
        ArgumentNullException.ThrowIfNull(services);

        var apiKeyOptions = new ApiKeyOptions();
        configureOptions?.Invoke(apiKeyOptions);

        services.AddSingleton(apiKeyOptions);
        services.AddScoped<IApiKeyService, ApiKeyService<TUser>>();

        return services;
    }
}
