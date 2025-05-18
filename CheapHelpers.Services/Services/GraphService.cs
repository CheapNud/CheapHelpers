using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace CheapHelpers.Services
{
	public class GraphService : IEmailService
	{
		public GraphService(string fromName, string fromAddress, string clientid, string tenantid, string clientsecret, bool indev, string[] devs)
		{
			FromName = fromName;
			FromAddress = fromAddress;
			TenantId = tenantid;
			ClientId = clientid;
			ClientSecret = clientsecret;
			InDev = indev;
			Developers = devs;
			_clientSecretCredential = new ClientSecretCredential(tenantId: TenantId, clientId: ClientId, clientSecret: ClientSecret);
			_appClient = new GraphServiceClient(_clientSecretCredential, new[] { "https://graph.microsoft.com/.default" });
		}

		private readonly ClientSecretCredential _clientSecretCredential;
		private readonly GraphServiceClient _appClient;

		public string FromName { get; }
		public string FromAddress { get; }
		public string TenantId { get; }
		public string ClientId { get; }
		public string ClientSecret { get; }
		public bool InDev { get; }
		public string[] Developers { get; }

		public async Task SendEmailAsync(string recipient, string subject, string body, (string, byte[])[] attachments = null, string[] cc = null, string[] bcc = null)
		{
			await SendEmailAsAsync(FromAddress, new string[] { recipient }, subject, body, attachments, cc, bcc);
		}

		public async Task SendEmailAsync(string[] recipients, string subject, string body, (string, byte[])[] attachments = null, string[] cc = null, string[] bcc = null)
		{
			await SendEmailAsAsync(FromAddress, recipients, subject, body, attachments, cc, bcc);
		}

		public async Task SendEmailAsAsync(string from, string recipient, string subject, string body, (string, byte[])[] attachments = null, string[] cc = null, string[] bcc = null)
		{
			await SendEmailAsAsync(from, new string[] { recipient }, subject, body, attachments, cc, bcc);
		}

		public async Task SendEmailAsAsync(string from, string[] recipients, string subject, string body, (string, byte[])[] attachments = null, string[] cc = null, string[] bcc = null)
		{
            ArgumentNullException.ThrowIfNull(recipients);
			ArgumentNullException.ThrowIfNullOrWhiteSpace(subject);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(body);

			if (string.IsNullOrWhiteSpace(from))
			{
				from = FromAddress;
			}

			if (InDev)
			{
				Debug.WriteLine($@"overwriting recipients in mail with developers, original recipients: {recipients.ToDelimitedString(",")}, cc: {cc?.ToDelimitedString(",")}, bcc: {bcc?.ToDelimitedString(",")}");
				recipients = Developers;
				bcc = null;
				cc = null;
			}

			try
			{

				var requestBody = new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
				{
					Message = new Message()
					{
						Subject = subject,
						Body = new ItemBody
						{
							ContentType = Microsoft.Graph.Models.BodyType.Html,
							Content = body
						},
						ToRecipients = recipients.Select(x => new Recipient
						{
							EmailAddress = new EmailAddress
							{
								Address = x
							},
						}).ToList(),
						CreatedDateTime = DateTime.UtcNow,
						//Sender = new Recipient { EmailAddress = new EmailAddress { Address = "info@mecam.be", Name = "Info" } },
					},
					SaveToSentItems = true
				};

				if (bcc != null)
				{
					requestBody.Message.BccRecipients = bcc?.Select(x => new Recipient
					{
						EmailAddress = new EmailAddress
						{
							Address = x
						}
					}).ToList();
				}

				if (cc != null)
				{
					requestBody.Message.CcRecipients = cc?.Select(x => new Recipient
					{
						EmailAddress = new EmailAddress
						{
							Address = x
						}
					}).ToList();
				}

				if (attachments != null)
				{
					requestBody.Message.Attachments = attachments.Select(x => new Microsoft.Graph.Models.Attachment
					{
						OdataType = "#microsoft.graph.fileAttachment",
						Name = x.Item1,
						AdditionalData = new Dictionary<string, object>()
							{
								{
									"contentBytes", x.Item2
								},
							}
					}).ToList();
				}

				await _appClient.Users[from].SendMail.PostAsync(requestBody);

				Debug.WriteLine($@"Succesfully sent mail from {from} to {recipients.ToDelimitedString(",")} with subject {subject}");
			}
			catch (ODataError ex)
			{
				Debug.WriteLine(ex.Error.Code);
				Debug.WriteLine(ex.Error.Message);
				throw;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
				throw;
			}
		}
	}
}
