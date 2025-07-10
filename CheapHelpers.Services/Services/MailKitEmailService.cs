using MimeKit;
using System.Diagnostics;
using System.Net;

namespace CheapHelpers.Services;

public class MailKitEmailService(
    string host,
    int smtpPort,
    string fromName,
    string fromAddress,
    string password,
    bool inDev,
    string[] developers,
    string? username = null,
    string? domain = null) : IEmailService
{
    private const int DefaultConnectTimeoutMs = 30000;
    private const string DefaultContentType = MimeMapping.KnownMimeTypes.Html;

    private readonly NetworkCredential _networkCredential = new(username ?? fromAddress, password, domain);

    public string Host { get; } = host;
    public int Port { get; } = smtpPort;
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
            ValidateConfiguration();

            var message = CreateMimeMessage(from, finalRecipients, finalCc, finalBcc, subject, body, attachments);
            await SendMessageAsync(message);

            Debug.WriteLine($"Successfully sent mail from {from} to {string.Join(",", finalRecipients)} with subject '{subject}'");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to send email from {from} to {string.Join(",", finalRecipients)}: {ex.Message}");
            throw;
        }
    }

    private (string[] recipients, string[]? cc, string[]? bcc) ApplyDevOverrides(
        string[] recipients, string[]? cc, string[]? bcc)
    {
        if (!InDev)
            return (recipients, cc, bcc);

        Debug.WriteLine($"Development mode: Overriding recipients. Original - To: {string.Join(",", recipients)}, CC: {cc?.Length ?? 0}, BCC: {bcc?.Length ?? 0}");
        return (Developers, null, null);
    }

    private static MimeMessage CreateMimeMessage(
        string from,
        string[] recipients,
        string[]? cc,
        string[]? bcc,
        string subject,
        string body,
        (string FileName, byte[] Content)[]? attachments)
    {
        var builder = new BodyBuilder
        {
            HtmlBody = body
        };

        // Add attachments if provided
        if (attachments?.Length > 0)
        {
            foreach (var (fileName, content) in attachments)
            {
                builder.Attachments.Add(fileName, content);
            }
        }

        var message = new MimeMessage
        {
            Subject = subject,
            Body = builder.ToMessageBody()
        };

        message.From.Add(new MailboxAddress("", from));
        message.To.AddRange(recipients.Select(MailboxAddress.Parse));

        if (cc?.Length > 0)
        {
            message.Cc.AddRange(cc.Select(MailboxAddress.Parse));
        }

        if (bcc?.Length > 0)
        {
            message.Bcc.AddRange(bcc.Select(MailboxAddress.Parse));
        }

        return message;
    }

    private async Task SendMessageAsync(MimeMessage message)
    {
        using var client = new MailKit.Net.Smtp.SmtpClient();

        try
        {
            client.CheckCertificateRevocation = false;
            client.Timeout = DefaultConnectTimeoutMs;

            await client.ConnectAsync(Host, Port, MailKit.Security.SecureSocketOptions.Auto);
            await client.AuthenticateAsync(_networkCredential);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SMTP error: {ex.Message}");
            throw;
        }
    }

    private void ValidateConfiguration()
    {
        if (_networkCredential is null)
        {
            throw new InvalidOperationException("Network credentials have not been provided");
        }

        if (string.IsNullOrWhiteSpace(FromAddress))
        {
            throw new InvalidOperationException("From address has not been provided");
        }

        if (string.IsNullOrWhiteSpace(Host))
        {
            throw new InvalidOperationException("SMTP host address has not been provided");
        }

        if (Port <= 0)
        {
            throw new InvalidOperationException("SMTP port must be greater than 0");
        }
    }
}