using Azure;
using Azure.AI.Translation.Document;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CheapHelpers.Services
{
	public class TranslatorService
	{
		public TranslatorService(string key, string endpoint, string documentendpoint)
		{
			_apikey = key;
			_endpoint = endpoint;
			_documentendpoint = documentendpoint;
		}

		private readonly string _apikey;
		private readonly string _endpoint;
		private readonly string _documentendpoint;

		public async Task<string> DirectTranslate(string textToTranslate, string to = "en", string from = null)
		{
			var result = await Translate(textToTranslate, to, from);
			return result.FirstOrDefault()?.Translations?.FirstOrDefault()?.Text;
		}

		public async Task<List<AzureTranslation>> Translate(string textToTranslate, string to = "en", string from = null)
		{
			if (string.IsNullOrWhiteSpace(textToTranslate))
			{
				throw new ArgumentException($"'{nameof(textToTranslate)}' cannot be null or whitespace.", nameof(textToTranslate));
			}

			try
			{
				// Input and output languages are defined as parameters.
				string route = $@"/translate?api-version=3.0{(from != null ? $@"&from={from}" : null)}&to={to}";
				object[] body = new object[] { new { Text = textToTranslate } };
				var requestBody = body.ToJson();

				using (var client = new HttpClient())
				{
					using (var request = new HttpRequestMessage())
					{
						// Build the request.
						request.Method = HttpMethod.Post;
						request.RequestUri = new Uri(_endpoint + route);
						request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
						request.Headers.Add("Ocp-Apim-Subscription-Key", _apikey);
						// location required if you're using a multi-service or regional (not global) resource.
						//request.Headers.Add("Ocp-Apim-Subscription-Region", location);
						// Send the request and get response.
						using (HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false))
						{
							// Read response as a string.
							string rawresult = await response.Content.ReadAsStringAsync();
							Debug.WriteLine(rawresult);
							var resultobj = rawresult.FromJson<List<AzureTranslation>>();
							return resultobj;
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
				throw;
			}
		}

		public async Task TranslateDocument(string sourceUrl, string destinationUrl)
		{
			Uri sourceUri = new Uri(sourceUrl);
			Uri targetUri = new Uri(destinationUrl);

			DocumentTranslationClient client = new DocumentTranslationClient(new Uri(_documentendpoint), new AzureKeyCredential(_apikey));
			DocumentTranslationInput input = new DocumentTranslationInput(sourceUri, targetUri, CultureInfo.CurrentCulture.TwoLetterISOLanguageName);
			DocumentTranslationOperation operation = await client.StartTranslationAsync(input);

			await operation.WaitForCompletionAsync();

			Debug.WriteLine($"Status: {operation.Status}");
			Debug.WriteLine($"Created on: {operation.CreatedOn}");
			Debug.WriteLine($"Last modified: {operation.LastModified}");
			Debug.WriteLine($"Total documents: {operation.DocumentsTotal}");
			Debug.WriteLine($"Succeeded: {operation.DocumentsSucceeded}");
			Debug.WriteLine($"Failed: {operation.DocumentsFailed}");
			Debug.WriteLine($"In Progress: {operation.DocumentsInProgress}");
			Debug.WriteLine($"Not started: {operation.DocumentsNotStarted}");

			//await foreach (DocumentStatusResult document in operation.Value)
			//{
			//    Debug.WriteLine($"Document with Id: {document.Id}");
			//    Debug.WriteLine($"Status:{document.Status}");
			//    if (document.Status == DocumentTranslationStatus.Succeeded)
			//    {
			//        Debug.WriteLine($"Translated Document Uri: {document.TranslatedDocumentUri}");
			//        Debug.WriteLine($"Translated to language: {document.TranslatedToLanguageCode}.");
			//        Debug.WriteLine($"Document source Uri: {document.SourceDocumentUri}");
			//    }
			//    else
			//    {
			//        Debug.WriteLine($"Error Code: {document.Error.Code}");
			//        Debug.WriteLine($"Message: {document.Error.Message}");
			//    }
			//}
		}
	}
}
