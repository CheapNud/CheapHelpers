using System.Text.Json.Serialization;

namespace CheapHelpers.Services.Auth.Plex;

/// <summary>
/// Plex user profile returned from the plex.tv API after successful authentication.
/// </summary>
public sealed record PlexUser
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("username")]
    public string Username { get; init; } = string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("thumb")]
    public string? Thumb { get; init; }

    [JsonPropertyName("authToken")]
    public string? AuthToken { get; init; }
}
