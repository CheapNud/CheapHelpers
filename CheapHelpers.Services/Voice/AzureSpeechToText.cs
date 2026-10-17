using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CheapHelpers.Services.Voice;

/// <summary>
/// Speech-to-text using the Azure Speech REST API for short audio.
/// Expects 16 kHz mono 16-bit PCM WAV. The endpoint rejects audio longer than 60 seconds,
/// returns only final results, and reports no confidence in the simple format, so a hit is reported as 1.0.
/// Failures are logged and return an empty <see cref="SttResult"/> rather than throwing.
/// </summary>
public sealed class AzureSpeechToText(HttpClient httpClient, IOptions<AzureSpeechOptions> options, ILogger<AzureSpeechToText> logger) : ISpeechToText
{
    // Content-Type value from the REST API docs; its "audio/pcm" parameter is not a valid header token, so it is added unvalidated.
    private const string WavContentType = "audio/wav; codecs=audio/pcm; samplerate=16000";

    public async Task<SttResult> RecognizeAsync(ReadOnlyMemory<byte> audio, SttOptions? sttOptions = null, CancellationToken cancellationToken = default)
    {
        var speechOptions = options.Value;
        var language = sttOptions?.Language ?? speechOptions.Language;

        if (string.IsNullOrWhiteSpace(speechOptions.Region) || string.IsNullOrWhiteSpace(speechOptions.SubscriptionKey))
        {
            logger.LogWarning("Azure Speech region or subscription key not configured, skipping transcription");
            return new SttResult(string.Empty, 0.0, language);
        }

        if (audio.IsEmpty)
            return new SttResult(string.Empty, 0.0, language);

        var endpoint = $"https://{speechOptions.Region}.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1" +
            $"?language={Uri.EscapeDataString(language)}&format=simple";

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
            httpRequest.Headers.Add("Ocp-Apim-Subscription-Key", speechOptions.SubscriptionKey);
            httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var audioContent = new ReadOnlyMemoryContent(audio);
            audioContent.Headers.TryAddWithoutValidation("Content-Type", WavContentType);
            httpRequest.Content = audioContent;

            using var httpResponse = await httpClient.SendAsync(httpRequest, cancellationToken);
            var responseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!httpResponse.IsSuccessStatusCode)
            {
                logger.LogWarning("Azure speech-to-text returned HTTP {Status}: {Body}", (int)httpResponse.StatusCode, responseBody);
                return new SttResult(string.Empty, 0.0, language);
            }

            using var doc = JsonDocument.Parse(responseBody);
            var recognitionStatus = doc.RootElement.TryGetProperty("RecognitionStatus", out var statusElement)
                ? statusElement.GetString()
                : null;

            // NoMatch, InitialSilenceTimeout, BabbleTimeout and Error all mean there is no text to return.
            if (recognitionStatus != "Success")
            {
                logger.LogDebug("Azure speech-to-text recognition status {RecognitionStatus}", recognitionStatus);
                return new SttResult(string.Empty, 0.0, language);
            }

            var transcribedText = doc.RootElement.TryGetProperty("DisplayText", out var textElement)
                ? textElement.GetString() ?? string.Empty
                : string.Empty;

            logger.LogDebug("Azure speech-to-text transcribed {Length} characters", transcribedText.Length);
            return new SttResult(transcribedText, 1.0, language);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Azure speech-to-text request failed");
            return new SttResult(string.Empty, 0.0, language);
        }
    }
}
