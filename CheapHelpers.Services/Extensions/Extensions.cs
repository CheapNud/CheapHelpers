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

        public static Task SendDeveloperAsync(this IEmailService emailSender, string body)
        {
            return emailSender.SendEmailAsync(emailSender.Developers, "Developer info", body);
        }

        public static Task SendDeveloperAsync(this IEmailService emailSender, Exception ex)
        {
            return emailSender.SendEmailAsync(emailSender.Developers, "Developer info exception", ex.ToHtmlString());
        }

        public static Task SendDeveloperAsync(this IEmailService emailSender, Exception ex, string[] param)
        {
            param.ForEach(x => x = $@"<p>{x}</p>");
            return emailSender.SendEmailAsync(emailSender.Developers, "Developer info exception", $@"{ex.ToHtmlString()}<br>parameters</p><br /><br /><br />{string.Concat(param)}");
        }

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
