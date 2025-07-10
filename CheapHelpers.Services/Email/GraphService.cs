using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using MoreLinq;
using System.Diagnostics;

namespace CheapHelpers.Services.Email;

public class GraphService(
    string fromName,
    string fromAddress,
    string clientId,
    string tenantId,
    string clientSecret,
    bool inDev,
    string[] developers) : IEmailService
{
    private const string DefaultScope = "https://graph.microsoft.com/.default";
    private const string FileAttachmentType = "#microsoft.graph.fileAttachment";

    private readonly ClientSecretCredential _clientSecretCredential = new(
        tenantId: tenantId,
        clientId: clientId,
        clientSecret: clientSecret);

    private readonly GraphServiceClient _appClient = new(
        new ClientSecretCredential(tenantId, clientId, clientSecret),
        [DefaultScope]);

    public string FromName { get; } = fromName;
    public string FromAddress { get; } = fromAddress;
    public string TenantId { get; } = tenantId;
    public string ClientId { get; } = clientId;
    public string ClientSecret { get; } = clientSecret;
    public bool InDev { get; } = inDev;
    public string[] Developers { get; } = developers;

    public async Task SendEmailAsync(string recipient, string subject, string body,
        (string FileName, byte[] Content)[] attachments = null, string[]? cc = null, string[]? bcc = null)
    {
        await SendEmailAsAsync(FromAddress, [recipient], subject, body, attachments, cc, bcc);
    }

    public async Task SendEmailAsync(string[] recipients, string subject, string body,
        (string FileName, byte[] Content)[] attachments = null, string[]? cc = null, string[]? bcc = null)
    {
        await SendEmailAsAsync(FromAddress, recipients, subject, body, attachments, cc, bcc);
    }

    public async Task SendEmailAsAsync(string from, string recipient, string subject, string body,
        (string FileName, byte[] Content)[] attachments = null, string[]? cc = null, string[]? bcc = null)
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
            var requestBody = CreateSendMailRequest(subject, body, finalRecipients, finalCc, finalBcc, attachments);
            await _appClient.Users[from].SendMail.PostAsync(requestBody);

            Debug.WriteLine($"Successfully sent mail from {from} to {finalRecipients.ToDelimitedString(",")} with subject '{subject}'");
        }
        catch (ODataError ex)
        {
            Debug.WriteLine($"Graph API Error - Code: {ex.Error?.Code}, Message: {ex.Error?.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unexpected error sending email: {ex.Message}");
            throw;
        }
    }

    private (string[] recipients, string[]? cc, string[]? bcc) ApplyDevOverrides(
        string[] recipients, string[]? cc, string[]? bcc)
    {
        if (!InDev)
            return (recipients, cc, bcc);

        Debug.WriteLine($"Development mode: Overriding recipients. Original - To: {recipients.ToDelimitedString(",")}, CC: {cc?.ToDelimitedString(",")}, BCC: {bcc?.ToDelimitedString(",")}");
        return (Developers, null, null);
    }

    private static Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody CreateSendMailRequest(
        string subject, string body, string[] recipients, string[]? cc, string[]? bcc,
        (string FileName, byte[] Content)[]? attachments)
    {
        var requestBody = new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
        {
            Message = new Message
            {
                Subject = subject,
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = body
                },
                ToRecipients = CreateRecipients(recipients),
                CreatedDateTime = DateTime.UtcNow,
            },
            SaveToSentItems = true
        };

        if (cc?.Length > 0)
            requestBody.Message.CcRecipients = CreateRecipients(cc);

        if (bcc?.Length > 0)
            requestBody.Message.BccRecipients = CreateRecipients(bcc);

        if (attachments?.Length > 0)
            requestBody.Message.Attachments = CreateAttachments(attachments);

        return requestBody;
    }

    private static List<Recipient> CreateRecipients(string[] addresses) =>
        addresses.Select(address => new Recipient
        {
            EmailAddress = new EmailAddress { Address = address }
        }).ToList();

    private static List<Attachment> CreateAttachments(
        (string FileName, byte[] Content)[] attachments) =>
        attachments.Select(attachment => new Attachment
        {
            OdataType = FileAttachmentType,
            Name = attachment.FileName,
            AdditionalData = new Dictionary<string, object>
            {
                { "contentBytes", attachment.Content }
            }
        }).ToList();
}