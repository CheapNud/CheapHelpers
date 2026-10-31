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
    /// <see cref="IDbContextFactory{TContext}"/> yourself.
    /// </summary>
    public static IServiceCollection AddCheapJobs<TPayload, TContext, TProcessor>(this IServiceCollection services, Action<JobQueueOptions>? configure = null)
        where TContext : DbContext
        where TProcessor : class, IJobProcessor<TPayload>
    {
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
