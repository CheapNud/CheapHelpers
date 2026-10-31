using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CheapHelpers.Jobs;

/// <summary>Registers the job queue and its worker.</summary>
public static class JobServiceCollectionExtensions
{
    /// <summary>
    /// Registers the options, <see cref="TimeProvider.System"/> when no clock is registered, the queue as a singleton,
    /// <typeparamref name="TProcessor"/> as scoped and the worker as a hosted service. Register
    /// <see cref="IDbContextFactory{TContext}"/> yourself. One queue per app: the options and the processor are shared registrations.
    /// </summary>
    /// <exception cref="InvalidOperationException">A queue is already registered.</exception>
    public static IServiceCollection AddCheapJobs<TPayload, TContext, TProcessor>(this IServiceCollection services, Action<JobQueueOptions>? configure = null)
        where TContext : DbContext
        where TProcessor : class, IJobProcessor<TPayload>
    {
        // A second queue would share the unnamed options and, on the same context, the job table, so it is refused
        if (services.Any(d => d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(JobQueue<,>)))
        {
            throw new InvalidOperationException("A job queue is already registered; one queue per app is supported.");
        }

        var optionsBuilder = services.AddOptions<JobQueueOptions>();
        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<JobQueue<TPayload, TContext>>();
        services.AddScoped<IJobProcessor<TPayload>, TProcessor>();
        services.AddHostedService<JobWorker<TPayload, TContext>>();
        return services;
    }
}
