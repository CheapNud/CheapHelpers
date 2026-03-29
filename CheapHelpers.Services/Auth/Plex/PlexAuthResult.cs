namespace CheapHelpers.Services.Auth.Plex;

/// <summary>
/// Result of a Plex authentication attempt, encapsulating success/failure with user data or error message.
/// </summary>
public sealed record PlexAuthResult(bool IsSuccess, PlexUser? User = null, string? ErrorMessage = null)
{
    public static PlexAuthResult Success(PlexUser plexUser) => new(true, plexUser);
    public static PlexAuthResult Failure(string errorMessage) => new(false, ErrorMessage: errorMessage);
}
