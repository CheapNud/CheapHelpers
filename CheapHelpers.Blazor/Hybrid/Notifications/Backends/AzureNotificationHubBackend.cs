using CheapHelpers.Blazor.Hybrid.Abstractions;
using CheapHelpers.Blazor.Hybrid.Models;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Blazor.Hybrid.Notifications.Backends;

/// <summary>
/// Azure Notification Hubs implementation of <see cref="IPushNotificationBackend"/>.
/// Manages device installations and sends push notifications via Azure NH.
/// </summary>
public class AzureNotificationHubBackend(
    string connectionString,
    string hubName,
    ILogger<AzureNotificationHubBackend>? logger = null) : IPushNotificationBackend
{
    private readonly NotificationHubClient _hubClient = NotificationHubClient.CreateClientFromConnectionString(connectionString, hubName);

    public async Task<bool> RegisterDeviceAsync(DeviceInstallation device)
    {
        try
        {
            var installation = new Installation
            {
                InstallationId = device.InstallationId,
                PushChannel = device.PushChannel,
                Platform = ParsePlatform(device.Platform),
                Tags = device.Tags,
            };

            await _hubClient.CreateOrUpdateInstallationAsync(installation);
            logger?.LogInformation("Registered device {InstallationId} on platform {Platform}", device.InstallationId, device.Platform);
            return true;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to register device {InstallationId}", device.InstallationId);
            return false;
        }
    }

    public async Task<DeviceInfo?> GetDeviceAsync(string deviceId)
    {
        try
        {
            var installation = await _hubClient.GetInstallationAsync(deviceId);
            return MapToDeviceInfo(installation);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to get device {DeviceId}", deviceId);
            return null;
        }
    }

    public async Task<List<DeviceInfo>> GetUserDevicesAsync(string userId)
    {
        var devices = new List<DeviceInfo>();
        try
        {
            // Query installations by user tag
            var tagExpression = $"user:{userId}";
            var registrations = await _hubClient.GetRegistrationsByTagAsync(tagExpression, 100);

            foreach (var registration in registrations)
            {
                devices.Add(new DeviceInfo
                {
                    DeviceId = registration.RegistrationId,
                    Platform = registration.GetType().Name.Replace("RegistrationDescription", ""),
                    UserId = userId,
                    IsActive = registration.ExpirationTime > DateTime.UtcNow,
                    Tags = registration.Tags?.ToList() ?? [],
                });
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to get devices for user {UserId}", userId);
        }

        return devices;
    }

    public async Task<bool> DeactivateDeviceAsync(string deviceId)
    {
        try
        {
            await _hubClient.DeleteInstallationAsync(deviceId);
            logger?.LogInformation("Deactivated device {DeviceId}", deviceId);
            return true;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to deactivate device {DeviceId}", deviceId);
            return false;
        }
    }

    public async Task<SendNotificationResult> SendNotificationAsync(NotificationPayload payload)
    {
        try
        {
            NotificationOutcome outcome;

            var notification = CreateNotification(payload);

            if (payload.Tags is { Count: > 0 })
            {
                var tagExpression = string.Join(" || ", payload.Tags);
                outcome = await _hubClient.SendNotificationAsync(notification, tagExpression);
            }
            else if (payload.DeviceIds is { Count: > 0 })
            {
                // Send to specific devices via their installation IDs
                outcome = await _hubClient.SendNotificationAsync(notification, payload.DeviceIds);
            }
            else
            {
                // Broadcast to all
                outcome = await _hubClient.SendNotificationAsync(notification);
            }

            logger?.LogInformation("Notification sent: {Success} success, {Failure} failure",
                outcome.Success, outcome.Failure);

            return new SendNotificationResult
            {
                Success = outcome.Failure == 0,
                SuccessCount = (int)outcome.Success,
                FailureCount = (int)outcome.Failure,
            };
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to send notification");
            return new SendNotificationResult
            {
                Success = false,
                ErrorMessage = ex.Message,
            };
        }
    }

    public async Task<SendNotificationResult> SendTestNotificationAsync(string deviceId)
    {
        return await SendNotificationAsync(new NotificationPayload
        {
            Title = "Test Notification",
            Body = "If you see this, push notifications are working!",
            DeviceIds = [deviceId],
            Data = new Dictionary<string, string> { ["test"] = "true" },
        });
    }

    private static NotificationPlatform ParsePlatform(string platform) => platform.ToLowerInvariant() switch
    {
        "fcm" or "fcmv1" or "android" => NotificationPlatform.FcmV1,
        "apns" or "ios" => NotificationPlatform.Apns,
        "wns" or "windows" => NotificationPlatform.Wns,
        "webpush" or "browser" => NotificationPlatform.Wns, // Web push via WNS for now
        _ => throw new ArgumentException($"Unknown platform: {platform}", nameof(platform)),
    };

    private static Notification CreateNotification(NotificationPayload payload)
    {
        // FCM v1 payload (Android + web)
        var fcmPayload = $$"""
            {
                "message": {
                    "notification": {
                        "title": "{{EscapeJson(payload.Title)}}",
                        "body": "{{EscapeJson(payload.Body)}}"
                    }{{(payload.Data is { Count: > 0 } ? $", \"data\": {System.Text.Json.JsonSerializer.Serialize(payload.Data)}" : "")}}{{(payload.Silent ? ", \"android\": { \"priority\": \"normal\" }" : "")}}
                }
            }
            """;

        // APNS payload (iOS)
        var apnsPayload = $$"""
            {
                "aps": {
                    {{(payload.Silent ? "\"content-available\": 1" : $"\"alert\": {{ \"title\": \"{EscapeJson(payload.Title)}\", \"body\": \"{EscapeJson(payload.Body)}\" }}")}}
                }{{(payload.Data is { Count: > 0 } ? $", {string.Join(", ", payload.Data.Select(kv => $"\"{EscapeJson(kv.Key)}\": \"{EscapeJson(kv.Value)}\""))}" : "")}}
            }
            """;

        // Return FCM as default — Azure NH routes to the correct platform based on installation
        return new FcmV1Notification(fcmPayload);
    }

    private static string EscapeJson(string input) =>
        input.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");

    private static DeviceInfo MapToDeviceInfo(Installation installation) => new()
    {
        DeviceId = installation.InstallationId,
        Platform = installation.Platform.ToString(),
        PushToken = installation.PushChannel,
        IsActive = true,
        Tags = installation.Tags?.ToList() ?? [],
        LastUpdated = DateTime.UtcNow,
    };
}
