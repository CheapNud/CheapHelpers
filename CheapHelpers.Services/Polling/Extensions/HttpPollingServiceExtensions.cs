using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.Services.Polling.Extensions;

/// <summary>
/// Extension methods for registering <see cref="IHttpPollingService{TResponse}"/> with DI.
/// Each TResponse type gets its own <see cref="HttpPollingOptions{TResponse}"/> singleton,
/// so multiple polling services can coexist with independent configurations.
/// </summary>
public static class HttpPollingServiceExtensions
{
    /// <summary>
    /// Registers an <see cref="IHttpPollingService{TResponse}"/> with the specified polling options.
    /// </summary>
    public static IServiceCollection AddHttpPolling<TResponse>(
        this IServiceCollection services,
        Action<HttpPollingOptions<TResponse>> configureOptions)
    {
        var pollingOptions = new HttpPollingOptions<TResponse>();
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
        HttpPollingOptions<TResponse> pollingOptions)
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
