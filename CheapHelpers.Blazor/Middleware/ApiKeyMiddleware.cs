using System.Collections.Concurrent;
using System.Security.Claims;
using CheapHelpers.Services.ApiKeys;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Blazor.Middleware;

/// <summary>
/// ASP.NET Core middleware that validates API keys from request headers and enforces rate limits.
/// Extracts the key from the configured header, validates via <see cref="IApiKeyService"/>,
/// and sets the <see cref="ClaimsPrincipal"/> with the key owner's identity and scopes.
/// </summary>
public class ApiKeyMiddleware(RequestDelegate next, ApiKeyOptions apiKeyOptions, ILogger<ApiKeyMiddleware> logger)
{
    private static readonly ConcurrentDictionary<string, SlidingWindow> RateLimitWindows = new();

    public async Task InvokeAsync(HttpContext httpContext)
    {
        var requestPath = httpContext.Request.Path;

        // Check if this path should be handled by API key middleware
        if (!ShouldProcess(requestPath))
        {
            await next(httpContext);
            return;
        }

        // Extract the API key from the configured header
        if (!httpContext.Request.Headers.TryGetValue(apiKeyOptions.HeaderName, out var headerValue) ||
            string.IsNullOrWhiteSpace(headerValue))
        {
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await httpContext.Response.WriteAsJsonAsync(new { error = $"Missing {apiKeyOptions.HeaderName} header" });
            return;
        }

        var rawKey = headerValue.ToString();
        var apiKeyService = httpContext.RequestServices.GetRequiredService<IApiKeyService>();
        var validationResult = await apiKeyService.ValidateAsync(rawKey, httpContext.RequestAborted);

        if (!validationResult.IsValid)
        {
            logger.LogWarning("API key validation failed: {Reason}", validationResult.FailureReason);
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await httpContext.Response.WriteAsJsonAsync(new { error = validationResult.FailureReason });
            return;
        }

        var apiKey = validationResult.Key!;

        // Rate limiting
        if (!CheckRateLimit(apiKey.KeyHash, apiKey.RateLimitPerMinute, apiKey.RateLimitPerDay, out var retryAfterSeconds))
        {
            logger.LogWarning("Rate limit exceeded for API key {KeyPrefix}", apiKey.KeyPrefix);
            httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            httpContext.Response.Headers["Retry-After"] = retryAfterSeconds.ToString();
            await httpContext.Response.WriteAsJsonAsync(new { error = "Rate limit exceeded", retryAfter = retryAfterSeconds });
            return;
        }

        // Set claims principal
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, apiKey.UserId),
            new("api_key_id", apiKey.Id.ToString()),
        };

        foreach (var scope in apiKey.Scopes)
            claims.Add(new Claim("scope", scope));

        var identity = new ClaimsIdentity(claims, "ApiKey");
        httpContext.User = new ClaimsPrincipal(identity);

        await next(httpContext);
    }

    private bool ShouldProcess(PathString requestPath)
    {
        // Check excluded paths first
        foreach (var excluded in apiKeyOptions.ExcludedPaths)
        {
            if (requestPath.StartsWithSegments(excluded, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        // If protected paths are specified, only process those
        if (apiKeyOptions.ProtectedPaths.Count > 0)
        {
            foreach (var protectedPath in apiKeyOptions.ProtectedPaths)
            {
                if (requestPath.StartsWithSegments(protectedPath, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        // No protected paths specified = protect everything (except excluded)
        return true;
    }

    private static bool CheckRateLimit(string keyHash, int limitPerMinute, int limitPerDay, out int retryAfterSeconds)
    {
        retryAfterSeconds = 0;

        // 0 means unlimited
        if (limitPerMinute == 0 && limitPerDay == 0)
            return true;

        var window = RateLimitWindows.GetOrAdd(keyHash, _ => new SlidingWindow());
        var now = DateTime.UtcNow;

        // Prune expired entries
        PruneQueue(window.MinuteEntries, now.AddMinutes(-1));
        PruneQueue(window.DayEntries, now.AddDays(-1));

        // Check minute limit
        if (limitPerMinute > 0 && window.MinuteEntries.Count >= limitPerMinute)
        {
            retryAfterSeconds = 60 - (int)(now - window.MinuteEntries.First()).TotalSeconds;
            if (retryAfterSeconds < 1) retryAfterSeconds = 1;
            return false;
        }

        // Check day limit
        if (limitPerDay > 0 && window.DayEntries.Count >= limitPerDay)
        {
            retryAfterSeconds = 86400 - (int)(now - window.DayEntries.First()).TotalSeconds;
            if (retryAfterSeconds < 1) retryAfterSeconds = 1;
            return false;
        }

        // Record this request
        window.MinuteEntries.Enqueue(now);
        window.DayEntries.Enqueue(now);

        return true;
    }

    private static void PruneQueue(ConcurrentQueue<DateTime> queue, DateTime cutoff)
    {
        while (queue.TryPeek(out var oldest) && oldest < cutoff)
            queue.TryDequeue(out _);
    }

    private sealed class SlidingWindow
    {
        public ConcurrentQueue<DateTime> MinuteEntries { get; } = new();
        public ConcurrentQueue<DateTime> DayEntries { get; } = new();
    }
}
