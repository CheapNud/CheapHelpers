using CheapHelpers.Models.Enums;
using CheapHelpers.Services.Notifications.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Services.Notifications.Examples;

#region Example Context

/// <summary>
/// EXAMPLE: Context for service-specific chat notifications.
/// </summary>
/// <remarks>
/// <para>
/// This demonstrates how to create a custom context that provides entity-specific information
/// to your subscription provider.
/// </para>
/// <para>
/// Consumers should create their own context classes implementing <see cref="ISubscriptionContext"/>
/// to pass relevant entity information (IDs, types, metadata) to their custom providers.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Usage in your notification service:
/// var context = new ServiceChatSubscriptionContext
/// {
///     ServiceId = 123,
///     ConversationId = 456
/// };
///
/// await notificationService.SendAsync(
///     userId: "user123",
///     notificationType: "ServiceChatMessage",
///     message: "New message in your service conversation",
///     context: context
/// );
/// </code>
/// </example>
public class ServiceChatSubscriptionContext : ISubscriptionContext
{
    /// <summary>
    /// Gets or sets the service identifier.
    /// </summary>
    /// <remarks>
    /// TODO: Replace with your own entity identifiers.
    /// </remarks>
    public int ServiceId { get; set; }

    /// <summary>
    /// Gets or sets the conversation identifier.
    /// </summary>
    /// <remarks>
    /// TODO: Add additional properties as needed for your entity.
    /// </remarks>
    public int ConversationId { get; set; }

    /// <inheritdoc />
    /// <remarks>
    /// TODO: Set this to match your entity type name.
    /// </remarks>
    public string EntityType => "ServiceChat";
}

#endregion

#region Example Entity

/// <summary>
/// EXAMPLE: Consumer's custom entity for storing service-specific notification subscriptions.
/// This would be added to the consumer's DbContext.
/// </summary>
/// <remarks>
/// <para>
/// This demonstrates the structure of a subscription entity that consumers would create
/// in their own database to store entity-specific notification preferences.
/// </para>
/// <para>
/// Consumers should:
/// <list type="bullet">
/// <item>Create this entity in their own DbContext</item>
/// <item>Add appropriate indexes for query performance</item>
/// <item>Configure relationships and constraints as needed</item>
/// <item>Add migration to create the table</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In your DbContext:
/// public DbSet&lt;ServiceNotificationSubscription&gt; ServiceNotificationSubscriptions { get; set; }
///
/// protected override void OnModelCreating(ModelBuilder modelBuilder)
/// {
///     modelBuilder.Entity&lt;ServiceNotificationSubscription&gt;(entity =>
///     {
///         entity.HasKey(e => e.Id);
///         entity.HasIndex(e => new { e.ServiceId, e.UserId, e.NotificationType });
///     });
/// }
/// </code>
/// </example>
public class ServiceNotificationSubscription
{
    // Consumer's custom entity in their DbContext

    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the service identifier this subscription is for.
    /// </summary>
    /// <remarks>
    /// TODO: Replace with your own entity identifier(s).
    /// </remarks>
    public int ServiceId { get; set; }

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification type (e.g., "ServiceChatMessage", "ServiceUpdate").
    /// </summary>
    public string NotificationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the enabled notification channels for this subscription.
    /// </summary>
    public NotificationChannelFlags EnabledChannels { get; set; }

    // TODO: Add additional properties as needed:
    // - CreatedAt, UpdatedAt timestamps
    // - IsActive flag
    // - Custom settings/metadata
    // - Relationships to other entities
}

#endregion

#region Example Provider

/// <summary>
/// EXAMPLE: Custom subscription provider for service-specific chat notifications.
/// This demonstrates how consumers can create entity-specific subscription logic
/// that overrides global user preferences.
/// </summary>
/// <remarks>
/// <para>
/// This example shows a complete implementation of a custom subscription provider
/// that allows users to have different notification preferences for specific services.
/// For example, a user might disable all chat notifications globally, but enable
/// them for critical services.
/// </para>
/// <para>
/// <strong>To implement your own custom provider:</strong>
/// </para>
/// <list type="number">
/// <item>
/// <description>
/// Create a context class implementing <see cref="ISubscriptionContext"/>
/// (see <see cref="ServiceChatSubscriptionContext"/>)
/// </description>
/// </item>
/// <item>
/// <description>
/// Create an entity in your DbContext to store subscriptions
/// (see <see cref="ServiceNotificationSubscription"/>)
/// </description>
/// </item>
/// <item>
/// <description>
/// Create a provider implementing <see cref="INotificationSubscriptionProvider"/>
/// (this class)
/// </description>
/// </item>
/// <item>
/// <description>
/// Register your provider:
/// <code>
/// services.AddScoped&lt;INotificationSubscriptionProvider, YourCustomProvider&gt;();
/// </code>
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>Priority System:</strong>
/// </para>
/// <para>
/// Providers are evaluated in priority order (highest first). This example uses
/// Priority = 10 to override global providers (which typically use Priority = 1).
/// Set your priority based on how you want providers to interact:
/// </para>
/// <list type="bullet">
/// <item>1000+: Override providers (user preferences, admin overrides)</item>
/// <item>500-999: Application-specific providers</item>
/// <item>100-499: Default providers</item>
/// <item>1-99: Fallback providers</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // 1. Register the provider in your Startup/Program.cs:
/// services.AddScoped&lt;INotificationSubscriptionProvider, ServiceChatSubscriptionProvider&gt;();
///
/// // 2. Create a subscription record in your database:
/// var subscription = new ServiceNotificationSubscription
/// {
///     ServiceId = 123,
///     UserId = "user123",
///     NotificationType = "ServiceChatMessage",
///     EnabledChannels = NotificationChannelFlags.InApp | NotificationChannelFlags.Push
/// };
/// context.ServiceNotificationSubscriptions.Add(subscription);
/// await context.SaveChangesAsync();
///
/// // 3. Send a notification with context:
/// var context = new ServiceChatSubscriptionContext
/// {
///     ServiceId = 123,
///     ConversationId = 456
/// };
///
/// await notificationService.SendAsync(
///     userId: "user123",
///     notificationType: "ServiceChatMessage",
///     message: "New message",
///     context: context
/// );
/// // The provider will use the service-specific subscription to determine channels
/// </code>
/// </example>
public class ServiceChatSubscriptionProvider(
    DbContext dbContext,
    ILogger<ServiceChatSubscriptionProvider> logger) : INotificationSubscriptionProvider
{
    /// <inheritdoc />
    /// <remarks>
    /// This provider uses priority 10 to override global providers (typically priority 1).
    /// TODO: Adjust priority based on your provider hierarchy needs.
    /// </remarks>
    public int Priority => 10;

    /// <inheritdoc />
    /// <remarks>
    /// TODO: Set a descriptive name for logging and diagnostics.
    /// </remarks>
    public string Name => "ServiceChat";

    /// <inheritdoc />
    /// <remarks>
    /// Only handles contexts of type <see cref="ServiceChatSubscriptionContext"/>.
    /// TODO: Modify to check for your custom context type.
    /// </remarks>
    public bool CanHandle(ISubscriptionContext? subscriptionContext)
    {
        return subscriptionContext is ServiceChatSubscriptionContext;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Queries the database for a service-specific subscription matching the ServiceId,
    /// UserId, and NotificationType. Returns the configured channels if found, or null
    /// to fall through to the next provider (typically global preferences).
    /// </para>
    /// <para>
    /// TODO: Modify the query logic to match your entity structure and business rules.
    /// </para>
    /// </remarks>
    public async Task<NotificationChannelFlags?> GetEnabledChannelsAsync(
        string userId,
        string notificationType,
        ISubscriptionContext? context,
        CancellationToken ct = default)
    {
        // Cast to our specific context type (already validated by CanHandle)
        var serviceContext = (ServiceChatSubscriptionContext)context!;

        logger.LogDebug(
            "Checking service chat subscription for User={UserId}, Service={ServiceId}, Type={NotificationType}",
            userId,
            serviceContext.ServiceId,
            notificationType);

        // TODO: Replace ServiceNotificationSubscription with your entity type
        // TODO: Modify the query to match your entity structure and filtering needs
        var subscription = await dbContext
            .Set<ServiceNotificationSubscription>()
            .Where(s => s.ServiceId == serviceContext.ServiceId
                        && s.UserId == userId
                        && s.NotificationType == notificationType)
            .Select(s => new { s.EnabledChannels })
            .FirstOrDefaultAsync(ct);

        if (subscription is not null)
        {
            logger.LogDebug(
                "Found service chat subscription with channels: {Channels}",
                subscription.EnabledChannels);

            return subscription.EnabledChannels;
        }

        logger.LogDebug("No service chat subscription found, falling through to next provider");

        // Return null to fall through to next provider (typically global preferences)
        return null;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// This example does not implement custom DND logic and returns null to use
    /// global DND settings. If you need entity-specific DND functionality, implement
    /// this method to query your custom DND configuration.
    /// </para>
    /// <para>
    /// TODO: Implement if you need custom Do Not Disturb logic for this entity type.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Example custom DND implementation:
    /// public async Task&lt;NotificationChannelFlags?&gt; GetDoNotDisturbChannelsAsync(
    ///     string userId,
    ///     CancellationToken ct = default)
    /// {
    ///     var dndSettings = await dbContext
    ///         .Set&lt;ServiceDndSettings&gt;()
    ///         .Where(s => s.UserId == userId &amp;&amp; s.IsActive)
    ///         .Select(s => s.DndChannels)
    ///         .FirstOrDefaultAsync(ct);
    ///
    ///     return dndSettings;
    /// }
    /// </code>
    /// </example>
    public Task<NotificationChannelFlags?> GetDoNotDisturbChannelsAsync(
        string userId,
        CancellationToken ct = default)
    {
        // Use global DND settings (return null)
        // TODO: Implement custom DND logic if needed
        return Task.FromResult<NotificationChannelFlags?>(null);
    }
}

#endregion
