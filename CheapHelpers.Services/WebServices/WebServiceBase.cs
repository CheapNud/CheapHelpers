using CheapHelpers.Services.WebServices.Configuration;
using Duende.IdentityModel.Client;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Text.Json;

namespace CheapHelpers.Services.WebServices;

public abstract class WebServiceBase : IWebServiceBase, IAsyncDisposable
{
    // Configuration Constants
    private const int DefaultTimeoutMinutes = 5;
    private const int MaxRetryAttempts = 3;
    private const int RetryDelayMs = 1000;
    private const string JsonContentType = "application/json";
    private const int TokenCacheExpiryMinutes = 55; // Cache tokens for 55 minutes (typical tokens are 60min)

    // Cache Keys
    private const string AccessTokenCacheKey = "AccessToken";

    private readonly HttpClient _httpClient;
    private readonly WebServiceOptions _options;
    private readonly IMemoryCache _tokenCache;
    private readonly ILogger<WebServiceBase>? _logger;
    private readonly bool _shouldDisposeHttpClient;

    public WebServiceBase(
        string serviceName,
        HttpClient? httpClient = null,
        WebServiceOptions? options = null,
        ILogger<WebServiceBase>? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        ServiceName = serviceName;
        _httpClient = httpClient ?? new HttpClient();
        _shouldDisposeHttpClient = httpClient is null;
        _options = options ?? new WebServiceOptions();
        _logger = logger;
        _tokenCache = new MemoryCache(new MemoryCacheOptions());

        // Configure HttpClient timeout
        _httpClient.Timeout = TimeSpan.FromMinutes(_options.TimeoutMinutes);

        _logger?.LogInformation("Created WebService for '{ServiceName}'. API URL: {ApiUrl}", ServiceName, ApiUrl);

        // Initialize SignalR if requested
        if (_options.CreateHub)
        {
            InitializeSignalRHub();
        }
    }

    public string ServiceName { get; }
    public HubConnection? HubConnection { get; private set; }
    public string Endpoint => _options.BaseEndpoint;
    public string HubUrl => $"{Endpoint}hub/{ServiceName}/";
    public string ApiEndpoint => $"{Endpoint}api/";
    public string ApiUrl => $"{ApiEndpoint}{ServiceName}/";

    #region SignalR Management

    public async Task StartHubAsync(CancellationToken cancellationToken = default)
    {
        if (HubConnection is null)
        {
            _logger?.LogWarning("Cannot start hub - HubConnection not initialized");
            return;
        }

        try
        {
            await HubConnection.StartAsync(cancellationToken);
            _logger?.LogInformation("Started SignalR connection on hub: {HubUrl}", HubUrl);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to start SignalR connection on hub: {HubUrl}", HubUrl);
            throw;
        }
    }

    public async Task StopHubAsync(CancellationToken cancellationToken = default)
    {
        if (HubConnection is null) return;

        try
        {
            await HubConnection.StopAsync(cancellationToken);
            _logger?.LogInformation("Stopped SignalR connection on hub: {HubUrl}", HubUrl);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error stopping SignalR connection: {Error}", ex.Message);
        }
    }

    #endregion

    #region HTTP Methods with Retry Logic

    public async Task<TResponse> GetAsync<TResponse>(
        string endpoint = "",
        CancellationToken cancellationToken = default) where TResponse : class
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            using var response = await _httpClient.GetAsync($"{ApiUrl}{endpoint}", cancellationToken);
            return await ProcessResponseAsync<TResponse>(response, cancellationToken);
        }, cancellationToken);
    }

    public async Task<TResponse> PostAsync<TResponse>(
        object? content = null,
        string endpoint = "",
        CancellationToken cancellationToken = default) where TResponse : class
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            using var httpContent = CreateJsonContent(content);
            using var response = await _httpClient.PostAsync($"{ApiUrl}{endpoint}", httpContent, cancellationToken);
            return await ProcessResponseAsync<TResponse>(response, cancellationToken);
        }, cancellationToken);
    }

    public async Task<TResponse> PutAsync<TResponse>(
        object? content = null,
        string endpoint = "",
        CancellationToken cancellationToken = default) where TResponse : class
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            using var httpContent = CreateJsonContent(content);
            using var response = await _httpClient.PutAsync($"{ApiUrl}{endpoint}", httpContent, cancellationToken);
            return await ProcessResponseAsync<TResponse>(response, cancellationToken);
        }, cancellationToken);
    }

    public async Task<TResponse> PatchAsync<TResponse>(
        object? content = null,
        string endpoint = "",
        CancellationToken cancellationToken = default) where TResponse : class
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            using var httpContent = CreateJsonContent(content);
            using var response = await _httpClient.PatchAsync($"{ApiUrl}{endpoint}", httpContent, cancellationToken);
            return await ProcessResponseAsync<TResponse>(response, cancellationToken);
        }, cancellationToken);
    }

    public async Task<TResponse> DeleteAsync<TResponse>(
        string endpoint = "",
        CancellationToken cancellationToken = default) where TResponse : class
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            using var response = await _httpClient.DeleteAsync($"{ApiUrl}{endpoint}", cancellationToken);
            return await ProcessResponseAsync<TResponse>(response, cancellationToken);
        }, cancellationToken);
    }

    public async Task DeleteAsync(string endpoint = "", CancellationToken cancellationToken = default)
    {
        await ExecuteWithRetryAsync(async () =>
        {
            using var response = await _httpClient.DeleteAsync($"{ApiUrl}{endpoint}", cancellationToken);
            response.EnsureSuccessStatusCode();
            return Task.CompletedTask;
        }, cancellationToken);
    }

    #endregion

    #region Authentication

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        // Check cache first
        if (_tokenCache.TryGetValue(AccessTokenCacheKey, out string? cachedToken) && !string.IsNullOrEmpty(cachedToken))
        {
            _logger?.LogDebug("Retrieved access token from cache");
            return cachedToken;
        }

        return await RequestAccessTokenAsync(cancellationToken);
    }

    private async Task<string> RequestAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenRequest = new ClientCredentialsTokenRequest
            {
                Address = _options.TokenEndpoint,
                ClientId = _options.ClientId,
                ClientSecret = _options.ClientSecret,
                Scope = _options.Scope,
            };

            var tokenResponse = await _httpClient.RequestClientCredentialsTokenAsync(tokenRequest, cancellationToken);

            if (tokenResponse.IsError)
            {
                var error = $"Token request failed: {tokenResponse.Error} - {tokenResponse.ErrorDescription}";
                _logger?.LogError("Token request failed: {Error}", error);
                throw new UnauthorizedAccessException(error);
            }

            // Cache the token (expires 5 minutes before actual expiry for safety)
            var cacheExpiry = TimeSpan.FromMinutes(TokenCacheExpiryMinutes);
            _tokenCache.Set(AccessTokenCacheKey, tokenResponse.AccessToken, cacheExpiry);

            _logger?.LogInformation("Successfully obtained and cached access token (expires in {Minutes} minutes)", TokenCacheExpiryMinutes);
            return tokenResponse.AccessToken!;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to obtain access token");
            throw;
        }
    }

    public async Task SetBearerTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.UseAuthentication) return;

        var token = await GetAccessTokenAsync(cancellationToken);
        _httpClient.SetBearerToken(token);
    }

    #endregion

    #region Private Helper Methods

    private void InitializeSignalRHub()
    {
        try
        {
            var hubBuilder = new HubConnectionBuilder()
                .WithUrl(HubUrl, _options.SignalRTransportType);

            if (_options.EnableSignalRLogging)
            {
                hubBuilder.ConfigureLogging(builder =>
                {
                    // Use the same logging configuration as the injected logger
                    // The consumer should configure logging in their DI container
                    builder.SetMinimumLevel(LogLevel.Debug);
                });
            }

            HubConnection = hubBuilder.Build();
            _logger?.LogInformation("Initialized SignalR hub connection: {HubUrl}", HubUrl);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize SignalR hub: {Error}", ex.Message);
            throw;
        }
    }

    private async Task<TResponse> ExecuteWithRetryAsync<TResponse>(
        Func<Task<TResponse>> operation,
        CancellationToken cancellationToken = default)
    {
        var attempt = 0;

        while (attempt < MaxRetryAttempts)
        {
            try
            {
                // Set authentication if required
                await SetBearerTokenAsync(cancellationToken);

                return await operation();
            }
            catch (HttpRequestException ex) when (attempt < MaxRetryAttempts - 1 && IsRetryableError(ex))
            {
                attempt++;
                var delay = RetryDelayMs * attempt;
                _logger?.LogWarning("HTTP request attempt {Attempt} failed, retrying in {Delay}ms. Error: {Error}",
                    attempt, delay, ex.Message);
                await Task.Delay(delay, cancellationToken);
            }
            catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger?.LogWarning("HTTP request was cancelled by user");
                throw;
            }
            catch (TaskCanceledException ex)
            {
                _logger?.LogError("HTTP request timed out after {Timeout} minutes: {Error}",
                    _options.TimeoutMinutes, ex.Message);
                throw new TimeoutException($"Request timed out after {_options.TimeoutMinutes} minutes", ex);
            }
        }

        throw new InvalidOperationException($"Request failed after {MaxRetryAttempts} attempts");
    }

    private static bool IsRetryableError(HttpRequestException ex)
    {
        // Retry on network errors, but not on client errors (4xx)
        return ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("network", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<TResponse> ProcessResponseAsync<TResponse>(
        HttpResponseMessage response,
        CancellationToken cancellationToken = default) where TResponse : class
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        _logger?.LogDebug("HTTP {Method} {Url} responded with {StatusCode} ({ContentLength} chars)",
            response.RequestMessage?.Method, response.RequestMessage?.RequestUri,
            response.StatusCode, content.Length);

        if (!response.IsSuccessStatusCode)
        {
            var error = CreateDetailedErrorMessage(response, content);
            _logger?.LogError("HTTP request failed: {Error}", error);

            throw response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => new UnauthorizedAccessException(error),
                HttpStatusCode.Forbidden => new UnauthorizedAccessException(error),
                HttpStatusCode.NotFound => new InvalidOperationException($"Resource not found: {error}"),
                HttpStatusCode.BadRequest => new ArgumentException($"Bad request: {error}"),
                _ => new HttpRequestException(error)
            };
        }

        // Handle string responses directly
        if (typeof(TResponse) == typeof(string))
        {
            return (content as TResponse)!;
        }

        // Deserialize JSON response
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Deserialize<TResponse>(content, options)
                   ?? throw new InvalidOperationException("Deserialization returned null");
        }
        catch (JsonException ex)
        {
            _logger?.LogError(ex, "Failed to deserialize response content: {Content}", content);
            throw new InvalidOperationException($"Failed to deserialize response: {ex.Message}", ex);
        }
    }

    private static string CreateDetailedErrorMessage(HttpResponseMessage response, string content)
    {
        return $"Status: {(int)response.StatusCode} {response.StatusCode}, " +
               $"URL: {response.RequestMessage?.RequestUri}, " +
               $"Content: {content}";
    }

    private static StringContent? CreateJsonContent(object? content)
    {
        if (content is null) return null;

        var json = JsonSerializer.Serialize(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return new StringContent(json, Encoding.UTF8, JsonContentType);
    }

    #endregion

    #region Disposal

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (HubConnection is not null)
            {
                await HubConnection.DisposeAsync();
            }

            _tokenCache?.Dispose();

            if (_shouldDisposeHttpClient)
            {
                _httpClient?.Dispose();
            }

            _logger?.LogInformation("WebService '{ServiceName}' disposed successfully", ServiceName);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during WebService disposal: {Error}", ex.Message);
        }
    }

    #endregion
}