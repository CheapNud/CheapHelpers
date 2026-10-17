using System.Security;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CheapHelpers.Services.Voice;

/// <summary>
/// Text-to-speech using the Azure Speech REST API (SSML in, audio in <see cref="AzureSpeechOptions.OutputFormat"/> out).
/// The service truncates audio longer than 10 minutes.
/// Failures are logged and return empty audio rather than throwing.
/// </summary>
public sealed class AzureTextToSpeech(HttpClient httpClient, IOptions<AzureSpeechOptions> options, ILogger<AzureTextToSpeech> logger) : ITextToSpeech
{
    // The REST API docs list User-Agent as a required header (application name, under 255 characters).
    private const string UserAgent = "CheapHelpers.Services";

    public async Task<ReadOnlyMemory<byte>> SynthesizeAsync(string text, TtsOptions? ttsOptions = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return ReadOnlyMemory<byte>.Empty;

        var speechOptions = options.Value;
        if (string.IsNullOrWhiteSpace(speechOptions.Region) || string.IsNullOrWhiteSpace(speechOptions.SubscriptionKey))
        {
            logger.LogWarning("Azure Speech region or subscription key not configured, skipping synthesis");
            return ReadOnlyMemory<byte>.Empty;
        }

        var voice = ttsOptions?.Voice ?? speechOptions.Voice;
        var language = ttsOptions?.Language ?? speechOptions.Language;
        var endpoint = $"https://{speechOptions.Region}.tts.speech.microsoft.com/cognitiveservices/v1";
        var ssml = $"<speak version='1.0' xml:lang='{SecurityElement.Escape(language)}'>" +
            $"<voice name='{SecurityElement.Escape(voice)}'>{SecurityElement.Escape(text)}</voice></speak>";

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
            httpRequest.Headers.Add("Ocp-Apim-Subscription-Key", speechOptions.SubscriptionKey);
            httpRequest.Headers.Add("X-Microsoft-OutputFormat", speechOptions.OutputFormat);
            httpRequest.Headers.UserAgent.ParseAdd(UserAgent);
            httpRequest.Content = new StringContent(ssml, Encoding.UTF8, "application/ssml+xml");

            using var httpResponse = await httpClient.SendAsync(httpRequest, cancellationToken);

            if (!httpResponse.IsSuccessStatusCode)
            {
                var errorBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("Azure text-to-speech returned HTTP {Status}: {Body}", (int)httpResponse.StatusCode, errorBody);
                return ReadOnlyMemory<byte>.Empty;
            }

            var audio = await httpResponse.Content.ReadAsByteArrayAsync(cancellationToken);
            logger.LogDebug("Azure text-to-speech synthesised {Bytes} bytes (voice {Voice})", audio.Length, voice);
            return audio;
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Azure text-to-speech request failed");
            return ReadOnlyMemory<byte>.Empty;
        }
    }
}
