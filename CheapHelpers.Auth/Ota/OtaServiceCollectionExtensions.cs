using CheapHelpers.Ota;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CheapHelpers.Auth.Ota;

/// <summary>
/// Registers the gateway update server: options, the release sources and <see cref="OtaUpdateChecker"/>.
/// </summary>
public static class OtaServiceCollectionExtensions
{
    private static readonly TimeSpan PooledConnectionLifetime = TimeSpan.FromMinutes(5);

    /// <summary>Registers the update server with options set in code; invalid options fail at startup.</summary>
    public static IServiceCollection AddCheapOta(this IServiceCollection services, Action<OtaOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        return services.AddCheapOta(services.AddOptions<OtaOptions>().Configure(configure));
    }

    /// <summary>Registers the update server with options bound from a configuration section; invalid options fail at startup.</summary>
    public static IServiceCollection AddCheapOta(this IServiceCollection services, IConfiguration section)
    {
        ArgumentNullException.ThrowIfNull(section);
        return services.AddCheapOta(services.AddOptions<OtaOptions>().Bind(section));
    }

    private static IServiceCollection AddCheapOta(this IServiceCollection services, OptionsBuilder<OtaOptions> optionsBuilder)
    {
        optionsBuilder.PostConfigure(otaOptions => otaOptions.Validate()).ValidateOnStart();

        services.AddMemoryCache();

        // The singleton source keeps its typed client for the app's lifetime, so pooled connections are recycled
        // instead of the handler, which keeps DNS changes visible
        services.AddHttpClient<ForgeReleaseSource>()
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { PooledConnectionLifetime = PooledConnectionLifetime })
            .SetHandlerLifetime(Timeout.InfiniteTimeSpan);
        services.AddHttpClient<UpstreamReleaseSource>()
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { PooledConnectionLifetime = PooledConnectionLifetime })
            .SetHandlerLifetime(Timeout.InfiniteTimeSpan);
        services.AddSingleton<LocalFolderReleaseSource>();

        services.AddSingleton<IOtaReleaseSource>(serviceProvider =>
            serviceProvider.GetRequiredService<IOptions<OtaOptions>>().Value.Source switch
            {
                OtaSourceKind.Forge => serviceProvider.GetRequiredService<ForgeReleaseSource>(),
                OtaSourceKind.Upstream => serviceProvider.GetRequiredService<UpstreamReleaseSource>(),
                OtaSourceKind.LocalFolder => serviceProvider.GetRequiredService<LocalFolderReleaseSource>(),
                var unknown => throw new InvalidOperationException($"{nameof(OtaOptions)}.{nameof(OtaOptions.Source)} has an unknown value {unknown}"),
            });
        services.AddSingleton<OtaUpdateChecker>();

        return services;
    }
}
