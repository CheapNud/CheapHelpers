using Azure;
using Azure.AI.Translation.Document;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;


namespace CheapHelpers.Services;

public class TranslatorService(
    string apiKey,
    string endpoint,
    string documentEndpoint,
    HttpClient? httpClient = null,
    ILogger<TranslatorService>? logger = null) : IDisposable
{
    private const string ApiVersion = "3.0";
    private const string DefaultTargetLanguage = "en";
    private const string SubscriptionKeyHeader = "Ocp-Apim-Subscription-Key";
    private const string JsonContentType = "application/json";
    private const int CacheExpiryMinutes = 60;
    private const int MaxRetryAttempts = 3;
    private const int RetryDelayMs = 1000;
    private const int MaxBatchSize = 100;

    private static readonly HashSet<string> ValidLanguageCodes = new()
    {
        "en", "nl", "fr", "de", "es", "it", "pt", "ru", "zh", "ja", "ko", "ar", "hi", "tr", "pl", "sv", "da", "no", "fi"
    };

    private readonly HttpClient _httpClient = httpClient ?? new HttpClient();
    private readonly bool _shouldDisposeHttpClient = httpClient is null;
    private readonly IMemoryCache _translationCache = new MemoryCache(new MemoryCacheOptions());
    private readonly ILogger<TranslatorService>? _logger = logger;

    public string ApiKey { get; } = apiKey;
    public string Endpoint { get; } = endpoint;
    public string DocumentEndpoint { get; } = documentEndpoint;

    /// <summary>
    /// Directly translates text and returns the first translation result
    /// </summary>
    public async Task<string?> DirectTranslateAsync(string textToTranslate, string to = DefaultTargetLanguage,
        string? from = null, CancellationToken cancellationToken = default)
    {
        var result = await TranslateAsync(textToTranslate, to, from, cancellationToken);
        return result.FirstOrDefault()?.Translations?.FirstOrDefault()?.Text;
    }

    /// <summary>
    /// Translates text and returns detailed translation results with caching support
    /// </summary>
    public async Task<List<AzureTranslation>> TranslateAsync(string textToTranslate, string to = DefaultTargetLanguage,
        string? from = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(textToTranslate);
        ValidateLanguageCode(to, nameof(to));
        if (from is not null) ValidateLanguageCode(from, nameof(from));

        var cacheKey = CreateCacheKey(textToTranslate, from, to);

        if (_translationCache.TryGetValue(cacheKey, out List<AzureTranslation>? cachedResult))
        {
            _logger?.LogDebug("Retrieved translation from cache for key: {CacheKey}", cacheKey);
            return cachedResult ?? [];
        }

        try
        {
            var result = await TranslateWithRetryAsync(textToTranslate, to, from, cancellationToken);

            // Cache successful results
            _translationCache.Set(cacheKey, result, TimeSpan.FromMinutes(CacheExpiryMinutes));

            _logger?.LogInformation("Successfully translated text from '{From}' to '{To}'. Results: {Count}",
                from ?? "auto-detect", to, result?.Count ?? 0);

            return result ?? [];
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to translate text from '{From}' to '{To}'", from ?? "auto-detect", to);
            throw;
        }
    }

    /// <summary>
    /// Translates multiple texts in a single batch operation
    /// </summary>
    public async Task<List<AzureTranslation>> TranslateBatchAsync(IEnumerable<string> textsToTranslate,
        string to = DefaultTargetLanguage, string? from = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(textsToTranslate);
        ValidateLanguageCode(to, nameof(to));
        if (from is not null) ValidateLanguageCode(from, nameof(from));

        var textList = textsToTranslate.ToList();
        if (textList.Count == 0)
        {
            return [];
        }

        if (textList.Count > MaxBatchSize)
        {
            throw new ArgumentException($"Batch size cannot exceed {MaxBatchSize} items", nameof(textsToTranslate));
        }

        try
        {
            var route = BuildTranslateRoute(to, from);
            var requestBody = CreateBatchTranslateRequestBody(textList);

            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(Endpoint + route));
            request.Content = new StringContent(requestBody, Encoding.UTF8, JsonContentType);
            request.Headers.Add(SubscriptionKeyHeader, ApiKey);

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var rawResult = await response.Content.ReadAsStringAsync(cancellationToken);
            var resultObject = rawResult.FromJson<List<AzureTranslation>>();

            _logger?.LogInformation("Successfully translated batch of {Count} texts from '{From}' to '{To}'",
                textList.Count, from ?? "auto-detect", to);

            return resultObject ?? [];
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to translate batch of {Count} texts", textList.Count);
            throw;
        }
    }

    /// <summary>
    /// Detects the language of the provided text
    /// </summary>
    public async Task<string?> DetectLanguageAsync(string text, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        try
        {
            const string route = $"/detect?api-version={ApiVersion}";
            var requestBody = CreateTranslateRequestBody(text);

            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(Endpoint + route));
            request.Content = new StringContent(requestBody, Encoding.UTF8, JsonContentType);
            request.Headers.Add(SubscriptionKeyHeader, ApiKey);

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var rawResult = await response.Content.ReadAsStringAsync(cancellationToken);
            var resultObject = rawResult.FromJson<List<LanguageDetectionResult>>();

            var detectedLanguage = resultObject?.FirstOrDefault()?.Language;
            _logger?.LogInformation("Detected language '{Language}' for provided text", detectedLanguage);

            return detectedLanguage;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to detect language for provided text");
            throw;
        }
    }

    /// <summary>
    /// Translates an entire document using Azure Document Translation service
    /// </summary>
    public async Task TranslateDocumentAsync(string sourceUrl, string destinationUrl, string? targetLanguage = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationUrl);

        try
        {
            var sourceUri = new Uri(sourceUrl);
            var targetUri = new Uri(destinationUrl);
            var language = targetLanguage ?? CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            ValidateLanguageCode(language, nameof(targetLanguage));

            var client = new DocumentTranslationClient(new Uri(DocumentEndpoint), new AzureKeyCredential(ApiKey));
            var input = new DocumentTranslationInput(sourceUri, targetUri, language);

            _logger?.LogInformation("Starting document translation from '{Source}' to '{Destination}' (target language: {Language})",
                sourceUrl, destinationUrl, language);

            var operation = await client.StartTranslationAsync(input, cancellationToken);
            await operation.WaitForCompletionAsync(cancellationToken);

            LogDocumentTranslationResults(operation);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to translate document from '{Source}' to '{Destination}'", sourceUrl, destinationUrl);
            throw;
        }
    }

    private async Task<List<AzureTranslation>> TranslateWithRetryAsync(string textToTranslate, string to, string? from,
        CancellationToken cancellationToken)
    {
        var attempt = 0;

        while (attempt < MaxRetryAttempts)
        {
            try
            {
                var route = BuildTranslateRoute(to, from);
                var requestBody = CreateTranslateRequestBody(textToTranslate);

                using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(Endpoint + route));
                request.Content = new StringContent(requestBody, Encoding.UTF8, JsonContentType);
                request.Headers.Add(SubscriptionKeyHeader, ApiKey);

                using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var rawResult = await response.Content.ReadAsStringAsync(cancellationToken);
                return rawResult.FromJson<List<AzureTranslation>>() ?? [];
            }
            catch (HttpRequestException) when (attempt < MaxRetryAttempts - 1)
            {
                attempt++;
                _logger?.LogWarning("Translation attempt {Attempt} failed, retrying in {Delay}ms", attempt, RetryDelayMs * attempt);
                await Task.Delay(RetryDelayMs * attempt, cancellationToken);
            }
        }

        throw new InvalidOperationException($"Translation failed after {MaxRetryAttempts} attempts");
    }

    private static void ValidateLanguageCode(string languageCode, string parameterName)
    {
        if (!ValidLanguageCodes.Contains(languageCode.ToLowerInvariant()))
        {
            throw new ArgumentException($"Unsupported language code: '{languageCode}'. Supported codes: {string.Join(", ", ValidLanguageCodes)}", parameterName);
        }
    }

    private static string CreateCacheKey(string text, string? from, string to)
    {
        var key = $"{text}|{from ?? "auto"}|{to}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
    }

    private static string BuildTranslateRoute(string to, string? from)
    {
        var fromParam = from is not null ? $"&from={from}" : string.Empty;
        return $"/translate?api-version={ApiVersion}{fromParam}&to={to}";
    }

    private static string CreateTranslateRequestBody(string textToTranslate)
    {
        var body = new TranslationRequest[] { new(textToTranslate) };
        return body.ToJson();
    }

    private static string CreateBatchTranslateRequestBody(IEnumerable<string> textsToTranslate)
    {
        var body = textsToTranslate.Select(text => new TranslationRequest(text)).ToArray();
        return body.ToJson();
    }

    private void LogDocumentTranslationResults(DocumentTranslationOperation operation)
    {
        var message = "Document translation completed with status: {Status}. " +
                     "Total: {Total}, Succeeded: {Succeeded}, Failed: {Failed}, In Progress: {InProgress}, Not Started: {NotStarted}";

        _logger?.LogInformation(message, operation.Status, operation.DocumentsTotal,
            operation.DocumentsSucceeded, operation.DocumentsFailed,
            operation.DocumentsInProgress, operation.DocumentsNotStarted);

        // Fallback to Debug.WriteLine if no logger
        if (_logger is null)
        {
            Debug.WriteLine($"Document translation completed with status: {operation.Status}");
            Debug.WriteLine($"Total documents: {operation.DocumentsTotal}, Succeeded: {operation.DocumentsSucceeded}, Failed: {operation.DocumentsFailed}");
        }
    }

    public void Dispose()
    {
        _translationCache?.Dispose();
        if (_shouldDisposeHttpClient)
        {
            _httpClient?.Dispose();
        }
    }

    private record TranslationRequest(string Text);
    private record LanguageDetectionResult(string Language, float Score);
}