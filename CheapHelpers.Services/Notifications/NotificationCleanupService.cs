using CheapHelpers.Services.Notifications.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CheapHelpers.Services.Notifications;

/// <summary>
/// Background service that periodically removes expired in-app notifications from the database.
/// </summary>
/// <remarks>
/// This service runs as a singleton and creates scoped service instances for each cleanup iteration
/// to properly integrate with Entity Framework DbContext lifetime management.
/// Cleanup runs every hour by default and deletes notifications past their ExpiresAt timestamp.
/// </remarks>
public class NotificationCleanupService(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<NotificationOptions> options,
    ILogger<NotificationCleanupService> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly NotificationOptions _options = options.Value;
    private readonly ILogger<NotificationCleanupService> _logger = logger;

    /// <summary>
    /// Executes the background cleanup task that runs until the application stops.
    /// </summary>
    /// <param name="stoppingToken">Token to signal when the service should stop.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Check if auto cleanup is enabled
        if (!_options.EnableAutoCleanup)
        {
            _logger.LogInformation("Notification cleanup service is disabled via configuration");
            return;
        }

        _logger.LogInformation("Notification cleanup service starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // TODO: Make cleanup interval configurable via NotificationOptions
                // Currently hardcoded to 1 hour
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);

                using var scope = _serviceScopeFactory.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<IInAppNotificationService>();

                var deletedCount = await notificationService.DeleteExpiredAsync(stoppingToken);

                if (deletedCount > 0)
                {
                    _logger.LogInformation("Deleted {Count} expired notifications", deletedCount);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error during notification cleanup");
            }
        }

        _logger.LogInformation("Notification cleanup service stopping");
    }
}
