using System.Threading.Tasks;

namespace CheapHelpers.Services
{
	public interface IEmailService
	{
		string FromName { get; }
		string FromAddress { get; }
		bool InDev { get; }
		string[] Developers { get; }
		Task SendEmailAsAsync(string from, string[] recipients, string subject, string body, (string, byte[])[] attachments = null, string[] cc = null, string[] bcc = null);
		Task SendEmailAsync(string recipient, string subject, string body, (string, byte[])[] attachments = null, string[] cc = null, string[] bcc = null);
		Task SendEmailAsync(string[] recipient, string subject, string body, (string, byte[])[] attachments = null, string[] cc = null, string[] bcc = null);
		Task SendEmailAsAsync(string from, string recipient, string subject, string body, (string, byte[])[] attachments = null, string[] cc = null, string[] bcc = null);
	}
}