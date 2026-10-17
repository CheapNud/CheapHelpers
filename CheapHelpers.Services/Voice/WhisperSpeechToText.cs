using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CheapHelpers.Services.Voice;

/// <summary>
/// Speech-to-text using OpenAI Whisper API (or any OpenAI-compatible endpoint, such as a local whisper.cpp server).
/// Failures are logged and return an empty <see cref="SttResult"/> rather than
/// throwing, so a misconfigured key or network blip doesn't tear down the voice
/// pipeline mid-session.
/// </summary>
public sealed class WhisperSpeechToText(HttpClient httpClient, IOptions<WhisperSttOptions> options, ILogger<WhisperSpeechToText> logger) : ISpeechToText
{
    public async Task<SttResult> RecognizeAsync(ReadOnlyMemory<byte> audio, SttOptions? sttOptions = null, CancellationToken cancellationToken = default)
    {
        var whisperOptions = options.Value;
        var language = sttOptions?.Language ?? whisperOptions.Language;
        var model = sttOptions?.Model ?? whisperOptions.Model;

        if (audio.IsEmpty)
            return new SttResult(string.Empty, 0.0, language);

        var endpoint = $"{whisperOptions.BaseUrl.TrimEnd('/')}/audio/transcriptions";

        try
        {
            using var formContent = new MultipartFormDataContent();
            var audioContent = new ReadOnlyMemoryContent(audio);
            audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
            formContent.Add(audioContent, "file", "audio.wav");
            formContent.Add(new StringContent(model), "model");
            if (!string.IsNullOrWhiteSpace(language))
                formContent.Add(new StringContent(language), "language");
            formContent.Add(new StringContent("json"), "response_format");

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
            // Local whisper.cpp servers usually run without auth, so the key is optional.
            if (!string.IsNullOrWhiteSpace(whisperOptions.ApiKey))
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", whisperOptions.ApiKey);
            httpRequest.Content = formContent;

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(whisperOptions.Timeout);

            using var httpResponse = await httpClient.SendAsync(httpRequest, timeoutCts.Token);
            var responseBody = await httpResponse.Content.ReadAsStringAsync(timeoutCts.Token);

            if (!httpResponse.IsSuccessStatusCode)
            {
                logger.LogWarning("Whisper transcription returned HTTP {Status}: {Body}", (int)httpResponse.StatusCode, responseBody);
                return new SttResult(string.Empty, 0.0, language);
            }

            using var doc = JsonDocument.Parse(responseBody);
            var transcribedText = doc.RootElement.TryGetProperty("text", out var textElement)
                ? textElement.GetString() ?? string.Empty
                : string.Empty;

            logger.LogDebug("Whisper transcribed {Length} characters", transcribedText.Length);
            return new SttResult(transcribedText, 1.0, language);
        }
        // The caller's cancellation propagates; our own timeout and every other failure return an empty result.
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Whisper transcription failed");
            return new SttResult(string.Empty, 0.0, language);
        }
    }
}
