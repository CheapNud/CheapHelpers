namespace CheapHelpers.Services.Email;

public interface IEmailService
{
    /// <summary>
    /// Developer email addresses for development mode and error notifications
    /// </summary>
    string[] Developers { get; }

    /// <summary>
    /// Sends an email to a single recipient using the default from address
    /// </summary>
    Task SendEmailAsync(string recipient, string subject, string body,
        (string FileName, byte[] Content)[]? attachments = null, string[]? cc = null, string[]? bcc = null);

    /// <summary>
    /// Sends an email to multiple recipients using the default from address
    /// </summary>
    Task SendEmailAsync(string[] recipients, string subject, string body,
        (string FileName, byte[] Content)[]? attachments = null, string[]? cc = null, string[]? bcc = null);

    /// <summary>
    /// Sends an email to a single recipient from a specific sender address
    /// </summary>
    Task SendEmailAsAsync(string? from, string recipient, string subject, string body,
        (string FileName, byte[] Content)[]? attachments = null, string[]? cc = null, string[]? bcc = null);

    /// <summary>
    /// Sends an email to multiple recipients from a specific sender address
    /// </summary>
    Task SendEmailAsAsync(string? from, string[] recipients, string subject, string body,
        (string FileName, byte[] Content)[]? attachments = null, string[]? cc = null, string[]? bcc = null);
}