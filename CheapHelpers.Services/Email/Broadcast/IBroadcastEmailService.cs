namespace CheapHelpers.Services.Email.Broadcast;

/// <summary>
/// Bulk email sending service with chunking, concurrency control, rate limiting, and progress tracking.
/// Built on top of <see cref="IEmailService"/> for actual delivery.
/// </summary>
public interface IBroadcastEmailService
{
    /// <summary>
    /// Sends a pre-rendered HTML body to all recipients.
    /// The same HTML is sent to every recipient.
    /// </summary>
    /// <param name="recipients">List of recipients to send to.</param>
    /// <param name="subject">Email subject line.</param>
    /// <param name="htmlBody">Pre-rendered HTML body content.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token to abort the broadcast.</param>
    Task<BroadcastResult> SendAsync(
        IReadOnlyList<BroadcastRecipient> recipients,
        string subject,
        string htmlBody,
        IProgress<BroadcastProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a Liquid template rendered per-recipient using each recipient's <see cref="BroadcastRecipient.TemplateData"/>.
    /// Template variables <c>Email</c> and <c>DisplayName</c> are always available, plus any keys from <c>TemplateData</c>.
    /// </summary>
    /// <param name="recipients">List of recipients with optional template data.</param>
    /// <param name="subject">Email subject line.</param>
    /// <param name="liquidTemplate">Liquid/Fluid template string to render per-recipient.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token to abort the broadcast.</param>
    Task<BroadcastResult> SendWithTemplateAsync(
        IReadOnlyList<BroadcastRecipient> recipients,
        string subject,
        string liquidTemplate,
        IProgress<BroadcastProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
