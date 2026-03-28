using System.Diagnostics;
using CheapHelpers.Services.Email.Sanitization;
using Fluid;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Services.Email.Broadcast;

/// <summary>
/// Bulk email sending service with chunking, concurrency control, rate limiting, and progress tracking.
/// Optionally sanitizes HTML when <see cref="IEmailHtmlSanitizer"/> is registered.
/// </summary>
public class BroadcastEmailService(
    IEmailService emailService,
    BroadcastEmailOptions broadcastOptions,
    ILogger<BroadcastEmailService> logger,
    IEmailHtmlSanitizer? sanitizer = null) : IBroadcastEmailService
{
    private static readonly FluidParser LiquidParser = new();

    public async Task<BroadcastResult> SendAsync(
        IReadOnlyList<BroadcastRecipient> recipients,
        string subject,
        string htmlBody,
        IProgress<BroadcastProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(recipients);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(htmlBody);

        // Sanitize once for the entire broadcast (same body for all)
        var sanitizedBody = SanitizeIfAvailable(htmlBody);

        return await SendCoreAsync(recipients, subject, _ => sanitizedBody, progress, cancellationToken);
    }

    public async Task<BroadcastResult> SendWithTemplateAsync(
        IReadOnlyList<BroadcastRecipient> recipients,
        string subject,
        string liquidTemplate,
        IProgress<BroadcastProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(recipients);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(liquidTemplate);

        if (!LiquidParser.TryParse(liquidTemplate, out var template, out var parseError))
            throw new ArgumentException($"Invalid Liquid template: {parseError}", nameof(liquidTemplate));

        return await SendCoreAsync(recipients, subject, recipient =>
        {
            var renderedHtml = RenderTemplate(template, recipient);
            return SanitizeIfAvailable(renderedHtml);
        }, progress, cancellationToken);
    }

    private async Task<BroadcastResult> SendCoreAsync(
        IReadOnlyList<BroadcastRecipient> recipients,
        string subject,
        Func<BroadcastRecipient, string> htmlBodyFactory,
        IProgress<BroadcastProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (recipients.Count == 0)
            return new BroadcastResult { TotalRecipients = 0, Results = [] };

        logger.LogInformation("Starting broadcast to {RecipientCount} recipients, subject: {Subject}", recipients.Count, subject);
        var stopwatch = Stopwatch.StartNew();

        var allResults = new List<BroadcastRecipientResult>(recipients.Count);
        var sentCount = 0;
        var failedCount = 0;
        var skippedCount = 0;

        var chunks = recipients.Chunk(broadcastOptions.ChunkSize);

        foreach (var chunk in chunks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var semaphore = new SemaphoreSlim(broadcastOptions.MaxConcurrency, broadcastOptions.MaxConcurrency);
            var chunkTasks = new List<Task<BroadcastRecipientResult>>(chunk.Length);

            foreach (var recipient in chunk)
            {
                await semaphore.WaitAsync(cancellationToken);

                chunkTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        return await SendToRecipientAsync(recipient, subject, htmlBodyFactory, cancellationToken);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken));
            }

            var chunkResults = await Task.WhenAll(chunkTasks);

            foreach (var chunkResult in chunkResults)
            {
                allResults.Add(chunkResult);

                if (chunkResult.IsSuccess) sentCount++;
                else if (chunkResult.IsSkipped) skippedCount++;
                else failedCount++;

                progress?.Report(new BroadcastProgress(
                    recipients.Count, sentCount, failedCount, skippedCount, chunkResult.Email));
            }

            // Rate limiting delay between chunks (skip after last chunk)
            if (broadcastOptions.ChunkDelayMs > 0)
                await Task.Delay(broadcastOptions.ChunkDelayMs, cancellationToken);
        }

        stopwatch.Stop();
        logger.LogInformation(
            "Broadcast complete: {Sent} sent, {Failed} failed, {Skipped} skipped in {Duration}",
            sentCount, failedCount, skippedCount, stopwatch.Elapsed);

        return new BroadcastResult
        {
            TotalRecipients = recipients.Count,
            SentCount = sentCount,
            FailedCount = failedCount,
            SkippedCount = skippedCount,
            Duration = stopwatch.Elapsed,
            Results = allResults,
        };
    }

    private async Task<BroadcastRecipientResult> SendToRecipientAsync(
        BroadcastRecipient recipient,
        string subject,
        Func<BroadcastRecipient, string> htmlBodyFactory,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(recipient.Email))
            return BroadcastRecipientResult.Skipped(recipient.Email ?? "", "Empty email address");

        string htmlBody;
        try
        {
            htmlBody = htmlBodyFactory(recipient);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Template rendering failed for {Email}", recipient.Email);
            return BroadcastRecipientResult.Failed(recipient.Email, $"Template rendering failed: {ex.Message}");
        }

        for (var attempt = 1; attempt <= broadcastOptions.MaxRetryAttempts; attempt++)
        {
            try
            {
                if (broadcastOptions.FromAddress is not null)
                    await emailService.SendEmailAsAsync(broadcastOptions.FromAddress, recipient.Email, subject, htmlBody);
                else
                    await emailService.SendEmailAsync(recipient.Email, subject, htmlBody);

                return BroadcastRecipientResult.Success(recipient.Email);
            }
            catch (Exception ex) when (attempt < broadcastOptions.MaxRetryAttempts)
            {
                var retryDelay = broadcastOptions.RetryDelayBaseMs * attempt;
                logger.LogWarning(ex,
                    "Send to {Email} failed (attempt {Attempt}/{Max}), retrying in {Delay}ms",
                    recipient.Email, attempt, broadcastOptions.MaxRetryAttempts, retryDelay);

                await Task.Delay(retryDelay, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Send to {Email} failed after {Attempts} attempts", recipient.Email, broadcastOptions.MaxRetryAttempts);
                return BroadcastRecipientResult.Failed(recipient.Email, ex.Message);
            }
        }

        // Should not reach here, but just in case
        return BroadcastRecipientResult.Failed(recipient.Email, "Exhausted all retry attempts");
    }

    private string SanitizeIfAvailable(string html)
    {
        if (sanitizer is null)
            return html;

        var sanitizationResult = sanitizer.Sanitize(html);
        return sanitizationResult.SanitizedHtml;
    }

    private static string RenderTemplate(IFluidTemplate template, BroadcastRecipient recipient)
    {
        var templateContext = new TemplateContext();

        templateContext.SetValue("Email", recipient.Email);
        templateContext.SetValue("DisplayName", recipient.DisplayName ?? "");

        if (recipient.TemplateData is not null)
        {
            foreach (var kvp in recipient.TemplateData)
                templateContext.SetValue(kvp.Key, kvp.Value);
        }

        return template.Render(templateContext);
    }
}
