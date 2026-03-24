using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Services.Polling.Extensions;

/// <summary>
/// Extension methods for registering <see cref="IHttpPollingService{TResponse}"/> with DI.
/// </summary>
public static class HttpPollingServiceExtensions
{
    /// <summary>
    /// Registers an <see cref="IHttpPollingService{TResponse}"/> with the specified polling options.
    /// </summary>
    public static IServiceCollection AddHttpPolling<TResponse>(
        this IServiceCollection services,
        Action<HttpPollingOptions> configureOptions)
    {
        var pollingOptions = new HttpPollingOptions();
        configureOptions(pollingOptions);
        pollingOptions.Validate();

        services.AddSingleton(pollingOptions);

        services.AddHttpClient<IHttpPollingService<TResponse>, HttpPollingService<TResponse>>(client =>
        {
            client.BaseAddress = pollingOptions.Endpoint;
            client.Timeout = pollingOptions.RequestTimeout;
        });

        return services;
    }

    /// <summary>
    /// Registers an <see cref="IHttpPollingService{TResponse}"/> using a pre-built options instance.
    /// </summary>
    public static IServiceCollection AddHttpPolling<TResponse>(
        this IServiceCollection services,
        HttpPollingOptions pollingOptions)
    {
        pollingOptions.Validate();
        services.AddSingleton(pollingOptions);

        services.AddHttpClient<IHttpPollingService<TResponse>, HttpPollingService<TResponse>>(client =>
        {
            client.BaseAddress = pollingOptions.Endpoint;
            client.Timeout = pollingOptions.RequestTimeout;
        });

        return services;
    }
}
