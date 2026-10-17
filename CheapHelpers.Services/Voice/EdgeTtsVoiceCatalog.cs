using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Services.Voice;

/// <summary>
/// Live fetcher for Microsoft Edge's TTS voices list. Calls the same upstream endpoint
/// the Edge browser uses (and that <c>rany2/edge-tts</c> shells around), parses the
/// JSON, and caches the result in memory for the rest of the app session so a voice
/// picker doesn't hit the network again.
/// </summary>
/// <remarks>
/// Failures (offline, throttled, JSON shape changed) return an empty list — a UI can
/// degrade gracefully to free-text entry so a network blip can't strand the user
/// without a way to change voice. Register as a singleton so the cache survives across
/// pages.
/// </remarks>
public sealed class EdgeTtsVoiceCatalog(IHttpClientFactory httpClientFactory, ILogger<EdgeTtsVoiceCatalog> logger)
{
    private const string VoicesListUrl =
        "https://speech.platform.bing.com/consumer/speech/synthesize/readaloud/voices/list?trustedclienttoken=" + EdgeTextToSpeech.TrustedClientToken;

    private readonly SemaphoreSlim _fetchGate = new(1, 1);
    private IReadOnlyList<EdgeTtsVoice>? _cachedVoices;

    /// <summary>
    /// Returns the cached voice list, or fetches it from upstream on the first successful call.
    /// Subsequent calls within the same process reuse the cache regardless of how long
    /// ago the first fetch was — voices change rarely, and the app restart cycle is
    /// fast enough that a "stale for one session" tradeoff is fine.
    /// </summary>
    public async Task<IReadOnlyList<EdgeTtsVoice>> GetVoicesAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedVoices is not null) return _cachedVoices;

        // Single-flight: parallel page loads shouldn't fire two
        // identical requests against Microsoft's gateway.
        await _fetchGate.WaitAsync(cancellationToken);
        try
        {
            if (_cachedVoices is not null) return _cachedVoices;
            var voices = await FetchAsync(cancellationToken);
            // Only a successful fetch is cached, so a failed one is retried on the next call.
            if (voices.Count > 0) _cachedVoices = voices;
            return voices;
        }
        finally
        {
            _fetchGate.Release();
        }
    }

    private async Task<IReadOnlyList<EdgeTtsVoice>> FetchAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, VoicesListUrl);
            // The voices endpoint is more lenient about headers than the synth endpoint
            // is, but matching the User-Agent we already send for synth makes the two
            // requests look like they came from the same Edge build — fewer fingerprint
            // surprises if Microsoft tightens validation later.
            request.Headers.UserAgent.ParseAdd(EdgeTextToSpeech.EdgeUserAgent);

            // Created per fetch from the factory so this singleton never pins one handler.
            var httpClient = httpClientFactory.CreateClient(nameof(EdgeTtsVoiceCatalog));
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Edge TTS voices/list returned {Status}, returning an empty voice list",
                    (int)response.StatusCode);
                return [];
            }

            var entries = await response.Content.ReadFromJsonAsync<List<VoicesListEntry>>(cancellationToken: cancellationToken);
            if (entries is null) return [];

            return entries
                .Where(e => !string.IsNullOrEmpty(e.ShortName))
                .Select(e => new EdgeTtsVoice(
                    ShortName: e.ShortName!,
                    Locale: e.Locale ?? string.Empty,
                    Gender: e.Gender ?? string.Empty,
                    FriendlyName: e.FriendlyName ?? string.Empty))
                .ToList();
        }
        // The caller's cancellation propagates.
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Edge TTS voices/list fetch failed, returning an empty voice list");
            return [];
        }
    }

    /// <summary>Upstream JSON shape — only the fields we actually render are mapped.</summary>
    private sealed class VoicesListEntry
    {
        [JsonPropertyName("ShortName")]
        public string? ShortName { get; set; }
        [JsonPropertyName("Locale")]
        public string? Locale { get; set; }
        [JsonPropertyName("Gender")]
        public string? Gender { get; set; }
        [JsonPropertyName("FriendlyName")]
        public string? FriendlyName { get; set; }
    }
}
