using System.Diagnostics;

namespace CheapHelpers.Services.Email;

/// <summary>
/// No-op email service that logs warnings instead of sending.
/// Auto-registered as fallback when no real email provider is configured.
/// Replace with SendGrid/MailKit/Graph when ready via normal DI override.
/// </summary>
public class NullEmailService : IEmailService
{
    public string[] Developers { get; } = [];

    public Task SendEmailAsync(string recipient, string subject, string body,
        (string FileName, byte[] Content)[]? attachments = null, string[]? cc = null, string[]? bcc = null)
    {
        Debug.WriteLine($"NullEmailService: Would send '{subject}' to {recipient} — no email provider configured");
        return Task.CompletedTask;
    }

    public Task SendEmailAsync(string[] recipients, string subject, string body,
        (string FileName, byte[] Content)[]? attachments = null, string[]? cc = null, string[]? bcc = null)
    {
        Debug.WriteLine($"NullEmailService: Would send '{subject}' to {recipients.Length} recipients — no email provider configured");
        return Task.CompletedTask;
    }

    public Task SendEmailAsAsync(string? from, string recipient, string subject, string body,
        (string FileName, byte[] Content)[]? attachments = null, string[]? cc = null, string[]? bcc = null)
    {
        Debug.WriteLine($"NullEmailService: Would send '{subject}' from {from} to {recipient} — no email provider configured");
        return Task.CompletedTask;
    }

    public Task SendEmailAsAsync(string? from, string[] recipients, string subject, string body,
        (string FileName, byte[] Content)[]? attachments = null, string[]? cc = null, string[]? bcc = null)
    {
        Debug.WriteLine($"NullEmailService: Would send '{subject}' from {from} to {recipients.Length} recipients — no email provider configured");
        return Task.CompletedTask;
    }
}
