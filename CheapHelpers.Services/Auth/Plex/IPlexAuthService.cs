namespace CheapHelpers.Services.Auth.Plex;

/// <summary>
/// Plex pin-based OAuth authentication service.
/// Handles PIN creation, polling, user retrieval, and optional server access validation.
/// </summary>
public interface IPlexAuthService : IExternalAuthProvider
{
    /// <summary>
    /// Creates a strong PIN on plex.tv for the OAuth handshake.
    /// </summary>
    Task<(long PinId, string PinCode)> CreatePinAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds the plex.tv auth redirect URL for the given PIN code and callback URL.
    /// </summary>
    string GetAuthRedirectUrl(string pinCode, string forwardUrl);

    /// <summary>
    /// Performs a single check on a PIN to see if the user has authenticated.
    /// Returns the PIN with <see cref="PlexPin.AuthToken"/> populated on success, or null if not yet authenticated.
    /// </summary>
    Task<PlexPin?> CheckPinAsync(long pinId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the Plex user profile using an auth token.
    /// </summary>
    Task<PlexUser?> GetUserAsync(string authToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a Plex user has access to the admin's server.
    /// Requires <see cref="PlexAuthOptions.AdminToken"/> to be configured.
    /// The server owner always has access; other users are checked against the server's user list.
    /// </summary>
    Task<bool> HasServerAccessAsync(long plexUserId, CancellationToken cancellationToken = default);
}
