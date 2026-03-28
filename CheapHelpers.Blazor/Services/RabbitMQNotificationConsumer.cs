using System.Text.Json;
using CheapHelpers.Blazor.Hubs;
using CheapHelpers.Models.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CheapHelpers.Blazor.Services;

/// <summary>
/// Background service that consumes notifications from RabbitMQ and forwards them
/// to locally connected SignalR clients. This enables multi-server notification delivery
/// while keeping SignalR as the client-facing transport.
/// </summary>
public class RabbitMQNotificationConsumer(
    string connectionString,
    IHubContext<NotificationHub> hubContext,
    ILogger<RabbitMQNotificationConsumer> logger) : BackgroundService
{
    private const string ExchangeName = "cheaphelpers.notifications";
    private const string UserRoutingKeyPrefix = "notification.user.";
    private const string BroadcastRoutingKey = "notification.broadcast";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("RabbitMQ notification consumer starting");

        IConnection? connection = null;
        IChannel? channel = null;

        try
        {
            var factory = new ConnectionFactory { Uri = new Uri(connectionString) };
            connection = await factory.CreateConnectionAsync(stoppingToken);
            channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await channel.ExchangeDeclareAsync(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: stoppingToken);

            // Create an exclusive queue for this server instance
            var queueDeclareResult = await channel.QueueDeclareAsync(
                queue: "",
                durable: false,
                exclusive: true,
                autoDelete: true,
                cancellationToken: stoppingToken);

            var queueName = queueDeclareResult.QueueName;

            // Bind to all user notifications and broadcasts
            await channel.QueueBindAsync(queueName, ExchangeName, $"{UserRoutingKeyPrefix}*", cancellationToken: stoppingToken);
            await channel.QueueBindAsync(queueName, ExchangeName, BroadcastRoutingKey, cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var notification = JsonSerializer.Deserialize<InAppNotification>(ea.Body.Span);
                    if (notification is null) return;

                    if (ea.RoutingKey == BroadcastRoutingKey)
                    {
                        await hubContext.Clients.All.SendAsync("ReceiveNotification", notification, stoppingToken);
                        logger.LogDebug("Forwarded broadcast notification {NotificationId} to SignalR", notification.Id);
                    }
                    else if (ea.RoutingKey.StartsWith(UserRoutingKeyPrefix))
                    {
                        var userId = ea.RoutingKey[UserRoutingKeyPrefix.Length..];
                        await hubContext.Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", notification, stoppingToken);
                        logger.LogDebug("Forwarded notification {NotificationId} to SignalR for user {UserId}", notification.Id, userId);
                    }

                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing RabbitMQ notification message");
                    await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken);
                }
            };

            await channel.BasicConsumeAsync(queueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

            logger.LogInformation("RabbitMQ notification consumer listening on queue {QueueName}", queueName);

            // Keep running until cancellation
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("RabbitMQ notification consumer stopping");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "RabbitMQ notification consumer encountered a fatal error");
            throw;
        }
        finally
        {
            if (channel is not null)
            {
                await channel.CloseAsync();
                channel.Dispose();
            }

            if (connection is not null)
            {
                await connection.CloseAsync();
                connection.Dispose();
            }
        }
    }
}
