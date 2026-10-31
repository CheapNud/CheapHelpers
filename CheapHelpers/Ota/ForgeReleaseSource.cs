using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CheapHelpers.Ota;

/// <summary>
/// Reads releases from the release API of a Gitea or Forgejo repository and proxies their assets.
/// The release list is cached for <see cref="OtaOptions.ReleaseCacheDuration"/>.
/// </summary>
public sealed class ForgeReleaseSource(HttpClient http, IOptions<OtaOptions> options, IMemoryCache cache) : IOtaReleaseSource
{
    private const string CacheKey = "CheapHelpers.Ota.ForgeReleases";
    private const string TokenScheme = "token";

    private readonly OtaOptions _options = options.Value;
    private readonly Uri _baseUri = WithTrailingSlash(
        http.BaseAddress?.ToString()
        ?? options.Value.ForgeRepoApiUrl
        ?? throw new InvalidOperationException($"{nameof(OtaOptions)}.{nameof(OtaOptions.ForgeRepoApiUrl)} is required"));

    /// <inheritdoc />
    /// <exception cref="HttpRequestException">The forge cannot be reached or answers the list call with an error.</exception>
    public async Task<IReadOnlyList<OtaRelease>> GetReleasesAsync(CancellationToken cancellationToken = default) =>
        await cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _options.ReleaseCacheDuration;
            return await FetchReleasesAsync(cancellationToken);
        }) ?? [];

    /// <inheritdoc />
    public async Task<Stream?> OpenAssetAsync(string version, string assetName, CancellationToken cancellationToken = default)
    {
        // The version lands in the URL, so anything that is not a plain version never reaches the forge
        if (!System.Version.TryParse(version, out _))
            return null;

        var tagUri = new Uri(_baseUri, $"releases/tags/{Uri.EscapeDataString(_options.TagPrefix + version)}");
        using var tagResponse = await SendAsync(tagUri, HttpCompletionOption.ResponseContentRead, cancellationToken);
        if (!tagResponse.IsSuccessStatusCode)
            return null;

        var release = await tagResponse.Content.ReadFromJsonAsync<ForgeRelease>(cancellationToken);
        var downloadUrl = release?.Assets?.FirstOrDefault(asset => asset.Name == assetName)?.BrowserDownloadUrl;
        if (string.IsNullOrEmpty(downloadUrl))
            return null;

        var assetResponse = await SendAsync(new Uri(_baseUri, downloadUrl), HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!assetResponse.IsSuccessStatusCode)
        {
            assetResponse.Dispose();
            return null;
        }

        // Disposing the returned stream releases the connection
        return await assetResponse.Content.ReadAsStreamAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<OtaRelease>> FetchReleasesAsync(CancellationToken cancellationToken)
    {
        List<ForgeRelease> forgeReleases = [];
        for (var page = 1; page <= _options.ForgeMaxPages; page++)
        {
            var pageUri = new Uri(_baseUri, $"releases?limit={_options.ForgePageSize}&page={page}");
            using var response = await SendAsync(pageUri, HttpCompletionOption.ResponseContentRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var pageReleases = await response.Content.ReadFromJsonAsync<List<ForgeRelease>>(cancellationToken) ?? [];
            forgeReleases.AddRange(pageReleases);
            if (pageReleases.Count < _options.ForgePageSize)
                break;
        }

        return forgeReleases
            .Select(ToRelease)
            .OfType<OtaRelease>()
            .OrderByDescending(release => release.Parsed)
            .ToList();
    }

    private OtaRelease? ToRelease(ForgeRelease forgeRelease)
    {
        var tag = forgeRelease.TagName;
        if (tag is null || !tag.StartsWith(_options.TagPrefix, StringComparison.Ordinal))
            return null;

        var version = tag[_options.TagPrefix.Length..];
        if (!System.Version.TryParse(version, out _))
            return null;

        var mandatory = forgeRelease.Body?.Contains(_options.MandatoryMarker, StringComparison.OrdinalIgnoreCase) ?? false;
        return new OtaRelease(version, tag, mandatory);
    }

    private async Task<HttpResponseMessage> SendAsync(Uri uri, HttpCompletionOption completion, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);

        // The token goes only to the forge itself, never to a download host somewhere else
        var sameHost = Uri.Compare(uri, _baseUri, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0;
        if (!string.IsNullOrEmpty(_options.ForgeToken) && sameHost)
            request.Headers.Authorization = new AuthenticationHeaderValue(TokenScheme, _options.ForgeToken);

        return await http.SendAsync(request, completion, cancellationToken);
    }

    private static Uri WithTrailingSlash(string url) => new(url.TrimEnd('/') + "/");

    private sealed record ForgeRelease(
        [property: JsonPropertyName("tag_name")] string? TagName,
        [property: JsonPropertyName("body")] string? Body,
        [property: JsonPropertyName("assets")] List<ForgeAsset>? Assets);

    private sealed record ForgeAsset(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("browser_download_url")] string? BrowserDownloadUrl);
}
