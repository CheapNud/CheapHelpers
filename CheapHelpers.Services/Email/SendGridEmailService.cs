using System.Diagnostics;
using System.Net;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace CheapHelpers.Services.Email;

public class SendGridEmailService(
    string fromName,
    string fromAddress,
    string apiKey,
    bool inDev,
    string[] developers) : IEmailService
{
    private readonly SendGridClient _client = new(apiKey);

    public string FromName { get; } = fromName;
    public string FromAddress { get; } = fromAddress;
    public bool InDev { get; } = inDev;
    public string[] Developers { get; } = developers;

    public async Task SendEmailAsync(string recipient, string subject, string body,
        (string FileName, byte[] Content)[]? attachments = null, string[]? cc = null, string[]? bcc = null)
    {
        await SendEmailAsAsync(FromAddress, [recipient], subject, body, attachments, cc, bcc);
    }

    public async Task SendEmailAsync(string[] recipients, string subject, string body,
        (string FileName, byte[] Content)[]? attachments = null, string[]? cc = null, string[]? bcc = null)
    {
        await SendEmailAsAsync(FromAddress, recipients, subject, body, attachments, cc, bcc);
    }

    public async Task SendEmailAsAsync(string? from, string recipient, string subject, string body,
        (string FileName, byte[] Content)[]? attachments = null, string[]? cc = null, string[]? bcc = null)
    {
        await SendEmailAsAsync(from, [recipient], subject, body, attachments, cc, bcc);
    }

    public async Task SendEmailAsAsync(string? from, string[] recipients, string subject, string body,
        (string FileName, byte[] Content)[]? attachments = null, string[]? cc = null, string[]? bcc = null)
    {
        ArgumentNullException.ThrowIfNull(recipients);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);

        from = string.IsNullOrWhiteSpace(from) ? FromAddress : from;

        var (finalRecipients, finalCc, finalBcc) = ApplyDevOverrides(recipients, cc, bcc);

        try
        {
            var msg = BuildMessage(from, finalRecipients, finalCc, finalBcc, subject, body, attachments);
            var sendGridResponse = await _client.SendEmailAsync(msg);

            if (!sendGridResponse.IsSuccessStatusCode)
            {
                var responseBody = await sendGridResponse.Body.ReadAsStringAsync();
                Debug.WriteLine($"SendGrid error {sendGridResponse.StatusCode}: {responseBody}");

                if (sendGridResponse.StatusCode == HttpStatusCode.TooManyRequests)
                    throw new InvalidOperationException($"SendGrid rate limited. Response: {responseBody}");

                throw new InvalidOperationException($"SendGrid returned {(int)sendGridResponse.StatusCode}: {responseBody}");
            }

            Debug.WriteLine($"Successfully sent mail via SendGrid from {from} to {string.Join(",", finalRecipients)} with subject '{subject}'");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            Debug.WriteLine($"Failed to send email via SendGrid from {from} to {string.Join(",", finalRecipients)}: {ex.Message}");
            throw;
        }
    }

    private SendGridMessage BuildMessage(
        string from,
        string[] recipients,
        string[]? cc,
        string[]? bcc,
        string subject,
        string body,
        (string FileName, byte[] Content)[]? attachments)
    {
        var msg = new SendGridMessage
        {
            From = new EmailAddress(from, FromName),
            Subject = subject,
            HtmlContent = body
        };

        foreach (var recipient in recipients)
        {
            msg.AddTo(new EmailAddress(recipient));
        }

        if (cc?.Length > 0)
        {
            foreach (var address in cc)
                msg.AddCc(new EmailAddress(address));
        }

        if (bcc?.Length > 0)
        {
            foreach (var address in bcc)
                msg.AddBcc(new EmailAddress(address));
        }

        if (attachments?.Length > 0)
        {
            foreach (var (fileName, content) in attachments)
            {
                msg.AddAttachment(fileName, Convert.ToBase64String(content));
            }
        }

        return msg;
    }

    private (string[] recipients, string[]? cc, string[]? bcc) ApplyDevOverrides(
        string[] recipients, string[]? cc, string[]? bcc)
    {
        if (!InDev)
            return (recipients, cc, bcc);

        Debug.WriteLine($"Development mode: Overriding recipients. Original - To: {string.Join(",", recipients)}, CC: {cc?.Length ?? 0}, BCC: {bcc?.Length ?? 0}");
        return (Developers, null, null);
    }
}
