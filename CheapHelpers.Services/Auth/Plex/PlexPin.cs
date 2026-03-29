using System.Text.Json.Serialization;

namespace CheapHelpers.Services.Auth.Plex;

/// <summary>
/// Represents a Plex OAuth PIN used during the authentication handshake.
/// <see cref="AuthToken"/> is populated once the user authenticates at plex.tv.
/// </summary>
public sealed record PlexPin
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("authToken")]
    public string? AuthToken { get; init; }
}
