using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace CheapHelpers.Ota;

/// <summary>
/// Reads releases from the public release endpoints of another instance. Not cached here, because the upstream caches.
/// </summary>
public sealed class UpstreamReleaseSource(HttpClient http, IOptions<OtaOptions> options) : IOtaReleaseSource
{
    private const string ReleasesPath = "releases";

    private readonly Uri _baseUri = new(
        (options.Value.UpstreamUrl
            ?? throw new InvalidOperationException($"{nameof(OtaOptions)}.{nameof(OtaOptions.UpstreamUrl)} is required")).TrimEnd('/') + "/");

    /// <inheritdoc />
    /// <exception cref="HttpRequestException">The upstream cannot be reached or answers with an error.</exception>
    public async Task<IReadOnlyList<OtaRelease>> GetReleasesAsync(CancellationToken cancellationToken = default)
    {
        var releases = await http.GetFromJsonAsync<List<OtaRelease>>(new Uri(_baseUri, ReleasesPath), cancellationToken) ?? [];
        return releases
            .Where(release => System.Version.TryParse(release.Version, out _))
            .OrderByDescending(release => release.Parsed)
            .ToList();
    }

    /// <inheritdoc />
    /// <exception cref="HttpRequestException">The upstream cannot be reached or answers with a server error.</exception>
    public async Task<Stream?> OpenAssetAsync(string version, string assetName, CancellationToken cancellationToken = default)
    {
        var assetUri = new Uri(_baseUri, $"{ReleasesPath}/{Uri.EscapeDataString(version)}/{Uri.EscapeDataString(assetName)}");
        var response = await http.GetAsync(assetUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadAsStreamAsync(cancellationToken);

        using (response)
        {
            // A missing release is a plain miss; an upstream that is down must surface as unavailable
            if (response.StatusCode >= HttpStatusCode.InternalServerError)
                response.EnsureSuccessStatusCode();

            return null;
        }
    }
}
