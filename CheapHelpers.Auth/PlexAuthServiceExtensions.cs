using System.Net.Http.Headers;
using CheapHelpers.Auth.Plex;
using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.Auth;

/// <summary>
/// Extension methods for registering Plex authentication services.
/// </summary>
public static class PlexAuthServiceExtensions
{
    private const string JsonMediaType = "application/json";

    /// <summary>
    /// Adds Plex SSO authentication services to the service collection.
    /// Registers <see cref="IPlexAuthService"/> with a typed <see cref="HttpClient"/>
    /// and <see cref="IExternalAuthProvider"/> for provider enumeration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Configuration action for <see cref="PlexAuthOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPlexAuth(
        this IServiceCollection services,
        Action<PlexAuthOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        var plexOptions = new PlexAuthOptions();
        configureOptions(plexOptions);

        services.AddSingleton(plexOptions);

        services.AddHttpClient<IPlexAuthService, PlexAuthService>(client =>
        {
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(JsonMediaType));
            client.DefaultRequestHeaders.Add(PlexConstants.Headers.ClientIdentifier, plexOptions.ClientIdentifier);
            client.DefaultRequestHeaders.Add(PlexConstants.Headers.Product, plexOptions.ProductName);
        });

        // Register as IExternalAuthProvider so consumers can enumerate all registered providers
        services.AddSingleton<IExternalAuthProvider>(sp => sp.GetRequiredService<IPlexAuthService>());

        return services;
    }
}
