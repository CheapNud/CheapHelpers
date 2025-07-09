using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using MoreLinq;

namespace CheapHelpers.Services
{
    public static class Extensions
    {
        public static Task SendEmailConfirmationAsync(this IEmailService emailSender, string email, string link)
        {
            try
            {
                return emailSender.SendEmailAsync(email, "Confirm your email", $"Please confirm your account by clicking this link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>");
            }
            catch (Exception)
            {
                Debug.WriteLine("Email failed in the extension");
                throw;
            }
        }

        public static Task SendPasswordTokenAsync(this IEmailService emailSender, string email, string link)
        {
            try
            {
                return emailSender.SendEmailAsync(email, "Forgotten password", $"Please reset your password by clicking this link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>");
            }
            catch (Exception)
            {
                Debug.WriteLine("Email failed in the extension");
                throw;
            }
        }

        private const string DeveloperInfoSubject = "Developer info";
        private const string DeveloperExceptionSubject = "Developer info exception";

        /// <summary>
        /// Sends a developer notification email with custom body content
        /// </summary>
        public static Task SendDeveloperAsync(this IEmailService emailSender, string body) =>
            emailSender.SendEmailAsync(emailSender.Developers, DeveloperInfoSubject, body);

        /// <summary>
        /// Sends a developer notification email with exception details
        /// </summary>
        public static Task SendDeveloperAsync(this IEmailService emailSender, Exception ex) =>
            emailSender.SendEmailAsync(emailSender.Developers, DeveloperExceptionSubject, ex.ToHtmlString());

        /// <summary>
        /// Sends a developer notification email with exception details and additional parameters
        /// </summary>
        public static Task SendDeveloperAsync(this IEmailService emailSender, Exception ex, string[] parameters)
        {
            var formattedParameters = parameters.Select(param => $"<p>{param}</p>");
            var parametersHtml = string.Join("", formattedParameters);
            var body = $"{ex.ToHtmlString()}<br><p>Parameters:</p><br><br><br>{parametersHtml}";

            return emailSender.SendEmailAsync(emailSender.Developers, DeveloperExceptionSubject, body);
        }

        //TODO: really old code, should be replcaced with templating engine
        public static string ToHtmlString(this Exception ex)
        {
            StringBuilder errormessage = new();

            errormessage.AppendLine($@"<p>Assembly: {Assembly.GetExecutingAssembly()}</p><br>");
            errormessage.AppendLine($@"<p>Source: {ex.Source}</p><br>");
            errormessage.AppendLine($@"<p>Time: {DateTime.Now:MM/dd/yyyy HH:mm}</p><br>");
            errormessage.AppendLine($@"<p>Exception Type: {ex.GetType().Name}</p><br>");
            errormessage.AppendLine($@"<p>Message: {ex.Message}</p><br>");
            errormessage.AppendLine($@"<p>StackTrace: {ex.StackTrace}</p><br>");

            while ((ex = ex.InnerException) != null)
            {
                errormessage.AppendLine($@"<br><p><b>InnerException<b></p><br>");
                errormessage.AppendLine($@"<p>Source: {ex.Source}</p><br>");
                errormessage.AppendLine($@"<p>Time: {DateTime.Now:MM/dd/yyyy HH:mm}</p><br>");
                errormessage.AppendLine($@"<p>Exception Type: {ex.GetType().Name}</p><br>");
                errormessage.AppendLine($@"<p>Message: {ex.Message}</p><br>");
                errormessage.AppendLine($@"<p>StackTrace: {ex.StackTrace}</p><br>");
            }

            return errormessage.ToString();
        }
    }
}
