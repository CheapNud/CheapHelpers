using CheapHelpers.Extensions;
using CheapHelpers.Models.Dtos.AddressSearch;
using System.Diagnostics;

namespace CheapHelpers.Services.Address;

public class AddressSearchService(string apiKey, string clientId, string endpoint, HttpClient? httpClient = null) : IDisposable
{
    private const string ApiVersion = "1.0";
    private const string DefaultCountryCodes = "BE,NL";
    private const bool DefaultTypeahead = true;
    private const string ClientIdHeader = "x-ms-client-id";

    private readonly HttpClient _httpClient = httpClient ?? new HttpClient();
    private readonly bool _shouldDisposeHttpClient = httpClient is null;

    public string ApiKey { get; } = apiKey;
    public string ClientId { get; } = clientId;
    public string Endpoint { get; } = endpoint;

    /// <summary>
    /// Performs fuzzy address search with default country codes (BE,NL) and typeahead enabled
    /// </summary>
    public async Task<List<Result>> FuzzyAddressSearchAsync(string searchText, CancellationToken cancellationToken = default) =>
        await FuzzyAddressSearchAsync(searchText, DefaultCountryCodes, DefaultTypeahead, cancellationToken);

    /// <summary>
    /// Performs fuzzy address search with specified parameters
    /// </summary>
    public async Task<List<Result>> FuzzyAddressSearchAsync(
        string searchText,
        string countryCodes = DefaultCountryCodes,
        bool typeahead = DefaultTypeahead,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchText);

        try
        {
            var requestUri = BuildRequestUri(searchText, countryCodes, typeahead);

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Add(ClientIdHeader, ClientId);

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var rawResult = await response.Content.ReadAsStringAsync(cancellationToken);
            var resultObject = rawResult.FromJson<Root>();

            Debug.WriteLine($"Address search completed successfully. Query: '{searchText}', Results: {resultObject.Results?.Count ?? 0}");

            return resultObject.Results ?? [];
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine($"HTTP error during address search: {ex.Message}");
            throw;
        }
        catch (TaskCanceledException ex)
        {
            Debug.WriteLine($"Address search request was cancelled: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unexpected error during address search: {ex.Message}");
            throw;
        }
    }

    private Uri BuildRequestUri(string searchText, string countryCodes, bool typeahead)
    {
        var query = $"api-version={ApiVersion}&query={Uri.EscapeDataString(searchText)}&countryset={countryCodes}&typeahead={typeahead.ToString().ToLowerInvariant()}&subscription-key={ApiKey}";
        return new Uri($"{Endpoint}/fuzzy/json?{query}");
    }

    public void Dispose()
    {
        if (_shouldDisposeHttpClient)
        {
            _httpClient?.Dispose();
        }
    }
}