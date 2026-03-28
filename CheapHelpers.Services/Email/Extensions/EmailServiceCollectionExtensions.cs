using CheapHelpers.Services.Email.Broadcast;
using CheapHelpers.Services.Email.Sanitization;
using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.Services.Email.Extensions;

/// <summary>
/// Extension methods for registering email sanitization and broadcast services.
/// </summary>
public static class EmailServiceCollectionExtensions
{
    /// <summary>
    /// Adds the email HTML sanitizer to the service collection.
    /// Provides <see cref="IEmailHtmlSanitizer"/> for validating and sanitizing user-authored HTML.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional configuration for <see cref="EmailHtmlSanitizerOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEmailHtmlSanitizer(
        this IServiceCollection services,
        Action<EmailHtmlSanitizerOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var sanitizerOptions = new EmailHtmlSanitizerOptions();
        configureOptions?.Invoke(sanitizerOptions);

        services.AddSingleton(sanitizerOptions);
        services.AddSingleton<IEmailHtmlSanitizer, EmailHtmlSanitizer>();

        return services;
    }

    /// <summary>
    /// Adds the broadcast email service to the service collection.
    /// Provides <see cref="IBroadcastEmailService"/> for bulk email sending with chunking and rate limiting.
    /// Requires <see cref="IEmailService"/> to be registered for actual delivery.
    /// Optionally uses <see cref="IEmailHtmlSanitizer"/> if registered (call <see cref="AddEmailHtmlSanitizer"/> first).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional configuration for <see cref="BroadcastEmailOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBroadcastEmail(
        this IServiceCollection services,
        Action<BroadcastEmailOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var broadcastOptions = new BroadcastEmailOptions();
        configureOptions?.Invoke(broadcastOptions);

        services.AddSingleton(broadcastOptions);
        services.AddScoped<IBroadcastEmailService, BroadcastEmailService>();

        return services;
    }
}
