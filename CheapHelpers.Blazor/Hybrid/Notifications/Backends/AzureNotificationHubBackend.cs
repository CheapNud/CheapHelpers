using CheapHelpers.Blazor.Hybrid.Abstractions;
using CheapHelpers.Blazor.Hybrid.Models;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CheapHelpers.Blazor.Hybrid.Notifications.Backends;

/// <summary>
/// Azure Notification Hubs implementation of <see cref="IPushNotificationBackend"/>.
/// Manages device installations and sends push notifications via Azure NH.
/// <para>
/// Browser (Web Push) support: the released Microsoft.Azure.NotificationHubs SDK (4.2.0) has no
/// Browser platform, so browser installations and sends go through the NH REST API directly
/// (api-version 2020-06). Opt in via <paramref name="enableBrowserPush"/> — requires VAPID
/// credentials configured on the hub's Browser (Web Push) blade.
/// </para>
/// </summary>
public class AzureNotificationHubBackend(
    string connectionString,
    string hubName,
    ILogger<AzureNotificationHubBackend>? logger = null,
    bool enableBrowserPush = false) : IPushNotificationBackend
{
    private const string RestApiVersion = "2020-06";
    private static readonly HttpClient _httpClient = new();

    private readonly NotificationHubClient _hubClient = NotificationHubClient.CreateClientFromConnectionString(connectionString, hubName);

    public async Task<bool> RegisterDeviceAsync(DeviceInstallation device)
    {
        if (IsBrowserPlatform(device.Platform))
        {
            return await RegisterBrowserDeviceAsync(device);
        }

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

            // Browser installations are invisible to the SDK send above — fan out via REST as well
            var browserOk = !enableBrowserPush || await SendBrowserNotificationAsync(payload);

            logger?.LogInformation("Notification sent: {Success} success, {Failure} failure",
                outcome.Success, outcome.Failure);

            return new SendNotificationResult
            {
                Success = outcome.Failure == 0 && browserOk,
                SuccessCount = (int)outcome.Success,
                FailureCount = (int)outcome.Failure,
                ErrorMessage = browserOk ? null : "Browser push send failed — see logs",
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

    #region Browser (Web Push) via REST — no SDK support in Microsoft.Azure.NotificationHubs 4.2.0

    private static bool IsBrowserPlatform(string platform) =>
        platform.ToLowerInvariant() is "webpush" or "browser";

    private async Task<bool> RegisterBrowserDeviceAsync(DeviceInstallation device)
    {
        if (!enableBrowserPush)
        {
            logger?.LogError("Browser installation {InstallationId} rejected — enable browser push on UseAzureNotificationHubs and configure VAPID credentials on the hub", device.InstallationId);
            return false;
        }

        if (device.BrowserSubscription is not { Endpoint.Length: > 0, P256dh.Length: > 0, Auth.Length: > 0 })
        {
            logger?.LogError("Browser installation {InstallationId} rejected — BrowserSubscription requires endpoint, p256dh and auth", device.InstallationId);
            return false;
        }

        try
        {
            var installationJson = JsonSerializer.Serialize(new
            {
                installationId = device.InstallationId,
                platform = "browser",
                pushChannel = device.BrowserSubscription,
                tags = device.Tags,
            });

            using var request = CreateRestRequest(HttpMethod.Put, $"installations/{Uri.EscapeDataString(device.InstallationId)}");
            request.Content = new StringContent(installationJson, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                logger?.LogError("Failed to register browser device {InstallationId}: {StatusCode} {Body}", device.InstallationId, response.StatusCode, responseBody);
                return false;
            }

            logger?.LogInformation("Registered browser device {InstallationId}", device.InstallationId);
            return true;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to register browser device {InstallationId}", device.InstallationId);
            return false;
        }
    }

    // NH audience sends are queued: the POST returns 201 on acceptance regardless of how many
    // installations match, so zero browser subscribers never fails the send — only real HTTP
    // errors (bad SAS, browser/VAPID credentials missing on the hub) return false here.
    private async Task<bool> SendBrowserNotificationAsync(NotificationPayload payload)
    {
        try
        {
            var bodyFields = new Dictionary<string, object> { ["title"] = payload.Title, ["body"] = payload.Body };
            if (payload.Data is { Count: > 0 })
            {
                bodyFields["data"] = payload.Data;
            }

            using var request = CreateRestRequest(HttpMethod.Post, "messages/");
            request.Content = new StringContent(JsonSerializer.Serialize(bodyFields), Encoding.UTF8, "application/json");
            request.Headers.Add("ServiceBusNotification-Format", "browser");

            var tagExpression = payload switch
            {
                { Tags.Count: > 0 } => string.Join(" || ", payload.Tags),
                { DeviceIds.Count: > 0 } => string.Join(" || ", payload.DeviceIds.Select(id => $"$InstallationId:{{{id}}}")),
                _ => null,
            };
            if (tagExpression is not null)
            {
                request.Headers.Add("ServiceBusNotification-Tags", tagExpression);
            }

            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                logger?.LogError("Browser push send failed: {StatusCode} {Body}", response.StatusCode, responseBody);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Browser push send failed");
            return false;
        }
    }

    private HttpRequestMessage CreateRestRequest(HttpMethod method, string relativePath)
    {
        var (endpoint, keyName, key) = ParseConnectionString(connectionString);
        var resourceUri = $"{endpoint}{hubName}/{relativePath}";
        var request = new HttpRequestMessage(method, $"{resourceUri}?api-version={RestApiVersion}");
        request.Headers.TryAddWithoutValidation("Authorization", CreateSasToken(resourceUri, keyName, key));
        return request;
    }

    private static (string Endpoint, string KeyName, string Key) ParseConnectionString(string connectionString)
    {
        string? endpoint = null, keyName = null, key = null;
        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var separatorIndex = part.IndexOf('=');
            if (separatorIndex < 0) continue;
            var name = part[..separatorIndex].Trim();
            var value = part[(separatorIndex + 1)..].Trim();
            switch (name.ToLowerInvariant())
            {
                case "endpoint": endpoint = value.Replace("sb://", "https://"); break;
                case "sharedaccesskeyname": keyName = value; break;
                case "sharedaccesskey": key = value; break;
            }
        }

        if (endpoint is null || keyName is null || key is null)
        {
            throw new ArgumentException("Connection string must contain Endpoint, SharedAccessKeyName and SharedAccessKey");
        }

        return (endpoint.EndsWith('/') ? endpoint : endpoint + "/", keyName, key);
    }

    private static string CreateSasToken(string resourceUri, string keyName, string key)
    {
        var expiry = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds();
        var stringToSign = $"{Uri.EscapeDataString(resourceUri)}\n{expiry}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
        return $"SharedAccessSignature sr={Uri.EscapeDataString(resourceUri)}&sig={Uri.EscapeDataString(signature)}&se={expiry}&skn={keyName}";
    }

    #endregion

    private static NotificationPlatform ParsePlatform(string platform) => platform.ToLowerInvariant() switch
    {
        "fcm" or "fcmv1" or "android" => NotificationPlatform.FcmV1,
        "apns" or "ios" => NotificationPlatform.Apns,
        "wns" or "windows" => NotificationPlatform.Wns,
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
