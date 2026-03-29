using System.Diagnostics;
using System.Text;
using System.Text.Json;
using CheapHelpers.Models.Entities;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace CheapHelpers.Services.Notifications.RealTime;

/// <summary>
/// RabbitMQ-based implementation of <see cref="INotificationRealTimeService"/>.
/// Publishes notifications to a RabbitMQ topic exchange for multi-server delivery.
/// A companion <see cref="RabbitMQNotificationConsumer"/> subscribes and forwards to SignalR locally.
/// </summary>
public class RabbitMQNotificationRealTimeService : INotificationRealTimeService, IAsyncDisposable
{
    private const string ExchangeName = "cheaphelpers.notifications";
    private const string UserRoutingKeyPrefix = "notification.user.";
    private const string BroadcastRoutingKey = "notification.broadcast";

    private readonly ILogger<RabbitMQNotificationRealTimeService> _logger;
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    private RabbitMQNotificationRealTimeService(
        IConnection connection,
        IChannel channel,
        ILogger<RabbitMQNotificationRealTimeService> logger)
    {
        _connection = connection;
        _channel = channel;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new instance with an established RabbitMQ connection.
    /// Uses async factory pattern because RabbitMQ.Client v7 channel creation is async.
    /// </summary>
    public static async Task<RabbitMQNotificationRealTimeService> CreateAsync(
        string connectionString,
        ILogger<RabbitMQNotificationRealTimeService> logger)
    {
        var factory = new ConnectionFactory { Uri = new Uri(connectionString) };
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        logger.LogInformation("RabbitMQ notification publisher connected to {Exchange}", ExchangeName);
        return new RabbitMQNotificationRealTimeService(connection, channel, logger);
    }

    public async Task NotifyUserAsync(string userId, InAppNotification notification, CancellationToken ct)
    {
        var routingKey = $"{UserRoutingKeyPrefix}{userId}";
        await PublishAsync(routingKey, notification, ct);
        _logger.LogDebug("Published notification {NotificationId} to RabbitMQ for user {UserId}", notification.Id, userId);
    }

    public async Task NotifyUsersAsync(IEnumerable<string> userIds, InAppNotification notification, CancellationToken ct)
    {
        foreach (var userId in userIds)
        {
            await NotifyUserAsync(userId, notification, ct);
        }
    }

    public async Task BroadcastAsync(InAppNotification notification, CancellationToken ct)
    {
        await PublishAsync(BroadcastRoutingKey, notification, ct);
        _logger.LogDebug("Broadcast notification {NotificationId} to RabbitMQ", notification.Id);
    }

    private async Task PublishAsync(string routingKey, InAppNotification notification, CancellationToken ct)
    {
        try
        {
            var messageBody = JsonSerializer.SerializeToUtf8Bytes(notification);

            var properties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                MessageId = notification.Id.ToString(),
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            await _channel.BasicPublishAsync(
                exchange: ExchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: messageBody,
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish notification {NotificationId} to RabbitMQ with routing key {RoutingKey}", notification.Id, routingKey);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _channel.CloseAsync();
            await _connection.CloseAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error disposing RabbitMQ connection: {ex.Message}");
        }

        _channel.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
