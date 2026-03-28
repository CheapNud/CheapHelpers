using CheapHelpers.Helpers.Logs;
using CheapHelpers.Models.Contracts;
using CheapHelpers.Services.Email.Configuration;
using CheapHelpers.Services.Email.Helpers;
using System.Diagnostics;

namespace CheapHelpers.Services.Email;

public static class EmailExtensions
{
    private const string DeveloperInfoSubject = "Developer info";
    private const string DeveloperExceptionSubject = "Developer info exception";

    private static readonly Lazy<EmailTemplateService> TemplateService = new(() =>
        new EmailTemplateService(
            [new TemplateSource(typeof(EmailTemplateService).Assembly, "CheapHelpers.Services.Email.Templates")],
            TemplateConfiguration.GetBuiltInTemplateTypes()));

    public static async Task SendEmailConfirmationAsync(this IEmailService emailSender, string email, string link)
    {
        var templateData = new EmailConfirmationTemplateData
        {
            ConfirmationLink = link,
            Recipient = email
        };

        var rendered = await RenderOrThrowAsync(templateData);
        await emailSender.SendEmailAsync(email, rendered.Subject, rendered.HtmlBody);
    }

    public static async Task SendPasswordTokenAsync(this IEmailService emailSender, string email, string link)
    {
        var templateData = new PasswordResetTemplateData
        {
            ResetLink = link,
            Recipient = email
        };

        var rendered = await RenderOrThrowAsync(templateData);
        await emailSender.SendEmailAsync(email, rendered.Subject, rendered.HtmlBody);
    }

    /// <summary>
    /// Sends a developer notification email with custom body content
    /// </summary>
    public static Task SendDeveloperAsync(this IEmailService emailSender, string body) =>
        emailSender.SendEmailAsync(emailSender.Developers, DeveloperInfoSubject, body);

    /// <summary>
    /// Sends a developer notification email with exception details
    /// </summary>
    public static async Task SendDeveloperAsync(this IEmailService emailSender, Exception ex)
    {
        var templateData = new ExceptionReportTemplateData(DeveloperExceptionSubject)
        {
            Report = ex.ExtractExceptionData()
        };

        var rendered = await RenderOrThrowAsync(templateData);
        await emailSender.SendEmailAsync(emailSender.Developers, rendered.Subject, rendered.HtmlBody);
    }

    /// <summary>
    /// Sends a developer notification email with exception details and additional parameters
    /// </summary>
    public static async Task SendDeveloperAsync(this IEmailService emailSender, Exception ex, string[] parameters)
    {
        var templateData = new ExceptionReportTemplateData(DeveloperExceptionSubject)
        {
            Report = ex.ExtractExceptionData(),
            AdditionalParameters = parameters
        };

        var rendered = await RenderOrThrowAsync(templateData);
        await emailSender.SendEmailAsync(emailSender.Developers, rendered.Subject, rendered.HtmlBody);
    }

    private static async Task<Models.Dtos.Email.EmailTemplateResult> RenderOrThrowAsync<T>(T templateData) where T : IEmailTemplateData
    {
        var rendered = await TemplateService.Value.RenderEmailAsync(templateData);

        if (!rendered.IsValid)
        {
            Debug.WriteLine($"Email template rendering failed: {rendered.ErrorMessage}");
            throw new InvalidOperationException($"Email template rendering failed: {rendered.ErrorMessage}");
        }

        return rendered;
    }
}
