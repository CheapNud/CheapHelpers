using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.Scheduling;

/// <summary>
/// Extension methods for registering <see cref="IScheduledTaskService"/> with DI.
/// </summary>
public static class ScheduledTaskServiceExtensions
{
    /// <summary>
    /// Adds the scheduled task service as a hosted service.
    /// Resolve <see cref="IScheduledTaskService"/> to register tasks at runtime.
    /// </summary>
    public static IServiceCollection AddScheduledTasks(this IServiceCollection services)
    {
        services.AddSingleton<ScheduledTaskService>();
        services.AddSingleton<IScheduledTaskService>(sp => sp.GetRequiredService<ScheduledTaskService>());
        services.AddHostedService(sp => sp.GetRequiredService<ScheduledTaskService>());
        return services;
    }
}
