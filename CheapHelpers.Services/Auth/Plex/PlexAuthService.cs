using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Services.Auth.Plex;

/// <summary>
/// Plex pin-based OAuth authentication service.
/// Uses a typed <see cref="HttpClient"/> with X-Plex-Product and X-Plex-Client-Identifier headers
/// pre-configured by the DI extension.
/// </summary>
public class PlexAuthService(HttpClient httpClient, PlexAuthOptions plexOptions, ILogger<PlexAuthService> logger) : IPlexAuthService, IDisposable
{
    private readonly SemaphoreSlim _ownerLock = new(1, 1);
    private long? _cachedOwnerId;

    public string ProviderName => "Plex";

    public async Task<(long PinId, string PinCode)> CreatePinAsync(CancellationToken cancellationToken = default)
    {
        using var pinRequest = new HttpRequestMessage(HttpMethod.Post, $"{PlexConstants.PlexApiBase}/pins");
        pinRequest.Content = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("strong", "true"),
        ]);

        var pinResponse = await httpClient.SendAsync(pinRequest, cancellationToken);
        pinResponse.EnsureSuccessStatusCode();

        var pin = await pinResponse.Content.ReadFromJsonAsync<PlexPin>(cancellationToken);
        if (pin is null)
            throw new InvalidOperationException("Failed to create Plex pin — null response from plex.tv");

        logger.LogInformation("Created Plex auth pin {PinId}", pin.Id);
        return (pin.Id, pin.Code);
    }

    public string GetAuthRedirectUrl(string pinCode, string forwardUrl)
    {
        return $"{PlexConstants.PlexAuthUrl}?clientID={plexOptions.ClientIdentifier}&code={pinCode}&forwardUrl={Uri.EscapeDataString(forwardUrl)}&context%5Bdevice%5D%5Bproduct%5D={plexOptions.ProductName}";
    }

    public async Task<PlexPin?> CheckPinAsync(long pinId, CancellationToken cancellationToken = default)
    {
        using var checkRequest = new HttpRequestMessage(HttpMethod.Get, $"{PlexConstants.PlexApiBase}/pins/{pinId}");

        var checkResponse = await httpClient.SendAsync(checkRequest, cancellationToken);
        if (!checkResponse.IsSuccessStatusCode)
            return null;

        var pin = await checkResponse.Content.ReadFromJsonAsync<PlexPin>(cancellationToken);
        return string.IsNullOrEmpty(pin?.AuthToken) ? null : pin;
    }

    public async Task<PlexUser?> GetUserAsync(string authToken, CancellationToken cancellationToken = default)
    {
        using var userRequest = new HttpRequestMessage(HttpMethod.Get, $"{PlexConstants.PlexApiBase}/user");
        userRequest.Headers.Add(PlexConstants.Headers.Token, authToken);

        var userResponse = await httpClient.SendAsync(userRequest, cancellationToken);
        if (!userResponse.IsSuccessStatusCode)
            return null;

        var plexUser = await userResponse.Content.ReadFromJsonAsync<PlexUser>(cancellationToken);
        if (plexUser is not null)
            logger.LogInformation("Plex user authenticated: {Username} (ID: {PlexId})", plexUser.Username, plexUser.Id);

        return plexUser;
    }

    public async Task<bool> HasServerAccessAsync(long plexUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(plexOptions.AdminToken))
        {
            logger.LogWarning("HasServerAccessAsync called but AdminToken is not configured — denying access");
            return false;
        }

        var ownerId = await GetOwnerIdAsync(cancellationToken);
        if (ownerId == plexUserId)
            return true;

        using var usersRequest = new HttpRequestMessage(HttpMethod.Get, PlexConstants.PlexUsersApi);
        usersRequest.Headers.Add(PlexConstants.Headers.Token, plexOptions.AdminToken);

        var usersResponse = await httpClient.SendAsync(usersRequest, cancellationToken);
        if (!usersResponse.IsSuccessStatusCode)
            return false;

        var xml = await usersResponse.Content.ReadAsStringAsync(cancellationToken);
        var doc = XDocument.Parse(xml);
        var userIdStr = plexUserId.ToString();

        return doc.Descendants("User").Any(u => u.Attribute("id")?.Value == userIdStr);
    }

    private async Task<long?> GetOwnerIdAsync(CancellationToken cancellationToken)
    {
        if (_cachedOwnerId.HasValue)
            return _cachedOwnerId;

        await _ownerLock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedOwnerId.HasValue)
                return _cachedOwnerId;

            using var ownerRequest = new HttpRequestMessage(HttpMethod.Get, $"{PlexConstants.PlexApiBase}/user");
            ownerRequest.Headers.Add(PlexConstants.Headers.Token, plexOptions.AdminToken);

            var ownerResponse = await httpClient.SendAsync(ownerRequest, cancellationToken);
            if (!ownerResponse.IsSuccessStatusCode)
                return null;

            var owner = await ownerResponse.Content.ReadFromJsonAsync<PlexUser>(cancellationToken);
            _cachedOwnerId = owner?.Id;
            return _cachedOwnerId;
        }
        finally
        {
            _ownerLock.Release();
        }
    }

    public void Dispose()
    {
        _ownerLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
