using CheapHelpers.Helpers.Logs;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;

namespace CheapHelpers.Services.Email
{
    public static class EmailExtensions
    {

        private const string DeveloperInfoSubject = "Developer info";
        private const string DeveloperExceptionSubject = "Developer info exception";


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

        //TODO: really old code, should be replaced with templating engine
        public static string ToHtmlString(this Exception ex)
        {
            var report = ex.ExtractExceptionData();
            return FormatExceptionReportAsHtml(report);
        }

        private static string FormatExceptionReportAsHtml(ExceptionReport report)
        {
            var errormessage = new StringBuilder();

            // Format main exception
            AppendExceptionAsHtml(errormessage, report.MainException);

            // Format inner exceptions
            foreach (var inner in report.InnerExceptions)
            {
                errormessage.AppendLine($@"<br><p><b>InnerException</b></p><br>");
                AppendExceptionAsHtml(errormessage, inner);
            }

            return errormessage.ToString();
        }

        private static void AppendExceptionAsHtml(StringBuilder sb, ExceptionDetails details)
        {
            sb.AppendLine($@"<p>Assembly: {details.AssemblyName}</p><br>");
            sb.AppendLine($@"<p>Source: {details.Source}</p><br>");
            sb.AppendLine($@"<p>Time: {details.Timestamp:MM/dd/yyyy HH:mm}</p><br>");
            sb.AppendLine($@"<p>Exception Type: {details.ExceptionType}</p><br>");
            sb.AppendLine($@"<p>Message: {details.Message}</p><br>");
            sb.AppendLine($@"<p>StackTrace: {details.StackTrace}</p><br>");
        }
    }
}
}
