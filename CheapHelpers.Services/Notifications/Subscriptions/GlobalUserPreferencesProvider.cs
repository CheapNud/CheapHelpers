using CheapHelpers.EF;
using CheapHelpers.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Services.Notifications.Subscriptions;

/// <summary>
/// Global user preferences provider that returns channel preferences from the UserNotificationPreferences table.
/// This is a fallback provider with the lowest priority (0) that is always available.
/// </summary>
/// <typeparam name="TUser">The user type derived from IdentityUser</typeparam>
public class GlobalUserPreferencesProvider<TUser>(
    CheapContext<TUser> dbContext,
    ILogger<GlobalUserPreferencesProvider<TUser>> logger) : INotificationSubscriptionProvider
    where TUser : IdentityUser
{
    /// <summary>
    /// Gets the priority of this provider. Returns 0 (lowest) as this is a fallback provider.
    /// </summary>
    public int Priority => 0;

    /// <summary>
    /// Gets the name of this provider for logging and diagnostics.
    /// </summary>
    public string Name => "GlobalUserPreferences";

    /// <summary>
    /// Determines whether this provider can handle the given subscription context.
    /// Always returns true as this is a global fallback provider.
    /// </summary>
    /// <param name="subscriptionContext">The context containing information about the subscription request.</param>
    /// <returns>Always returns true.</returns>
    public bool CanHandle(ISubscriptionContext? subscriptionContext)
    {
        return true;
    }

    /// <summary>
    /// Gets the enabled notification channels for a specific user and notification type.
    /// Queries the UserNotificationPreferences table to retrieve user-specific channel preferences.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="notificationType">The type of notification being sent.</param>
    /// <param name="context">Optional context containing additional information about the subscription request.</param>
    /// <param name="ct">Cancellation token for async operations.</param>
    /// <returns>
    /// The enabled channels if a preference is found, or null if no preference exists.
    /// When null is returned, the resolver will apply default notification channels.
    /// </returns>
    public async Task<NotificationChannelFlags?> GetEnabledChannelsAsync(
        string userId,
        string notificationType,
        ISubscriptionContext? context,
        CancellationToken ct = default)
    {
        logger.LogDebug(
            "Querying global user preferences for UserId={UserId}, NotificationType={NotificationType}",
            userId,
            notificationType);

        var preference = await dbContext.UserNotificationPreferences
            .Where(p => p.UserId == userId && p.NotificationType == notificationType)
            .Select(p => p.EnabledChannels)
            .FirstOrDefaultAsync(ct);

        if (preference != default(NotificationChannelFlags))
        {
            logger.LogDebug(
                "Found preference for UserId={UserId}, NotificationType={NotificationType}: {Channels}",
                userId,
                notificationType,
                preference);

            return preference;
        }

        logger.LogDebug(
            "No preference found for UserId={UserId}, NotificationType={NotificationType}, returning null",
            userId,
            notificationType);

        return null;
    }

    /// <summary>
    /// Gets the channels that are currently in "Do Not Disturb" mode for a user.
    /// Checks the user's DND time window and returns channels to disable if currently in DND period.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="ct">Cancellation token for async operations.</param>
    /// <returns>
    /// The channels to disable (Push | Sms) if currently in DND period, or null if not in DND.
    /// </returns>
    public async Task<NotificationChannelFlags?> GetDoNotDisturbChannelsAsync(
        string userId,
        CancellationToken ct = default)
    {
        logger.LogDebug("Checking Do Not Disturb settings for UserId={UserId}", userId);

        var preferences = await dbContext.UserNotificationPreferences
            .Where(p => p.UserId == userId
                     && p.DoNotDisturbStartHour.HasValue
                     && p.DoNotDisturbEndHour.HasValue)
            .Select(p => new { p.DoNotDisturbStartHour, p.DoNotDisturbEndHour })
            .FirstOrDefaultAsync(ct);

        if (preferences == null)
        {
            logger.LogDebug("No DND settings found for UserId={UserId}", userId);
            return null;
        }

        // TODO: Use user's timezone instead of UTC - for now using UTC as a simplification
        var currentHour = DateTime.UtcNow.Hour;
        var startHour = preferences.DoNotDisturbStartHour!.Value;
        var endHour = preferences.DoNotDisturbEndHour!.Value;

        bool isInDndPeriod;

        if (startHour < endHour)
        {
            // DND period doesn't cross midnight (e.g., 9 AM to 5 PM)
            isInDndPeriod = currentHour >= startHour && currentHour < endHour;
        }
        else
        {
            // DND period crosses midnight (e.g., 10 PM to 2 AM)
            // If start=22 and end=2, then hour>=22 OR hour<2 means in DND
            isInDndPeriod = currentHour >= startHour || currentHour < endHour;
        }

        if (isInDndPeriod)
        {
            var dndChannels = NotificationChannelFlags.Push | NotificationChannelFlags.Sms;
            logger.LogDebug(
                "User {UserId} is in DND period (Start={Start}, End={End}, CurrentHour={CurrentHour}), disabling channels: {Channels}",
                userId,
                startHour,
                endHour,
                currentHour,
                dndChannels);

            return dndChannels;
        }

        logger.LogDebug(
            "User {UserId} is NOT in DND period (Start={Start}, End={End}, CurrentHour={CurrentHour})",
            userId,
            startHour,
            endHour,
            currentHour);

        return null;
    }
}
