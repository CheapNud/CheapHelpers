using System.Text.Json.Serialization;

namespace CheapHelpers.Ota;

/// <summary>
/// One published gateway release.
/// </summary>
/// <param name="Version">The version without the tag prefix, for example <c>1.4.2</c>.</param>
/// <param name="Tag">The full release tag, for example <c>v1.4.2</c>.</param>
/// <param name="Mandatory">True when the release must be installed even by a pinned device.</param>
public sealed record OtaRelease(
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("tag")] string Tag,
    [property: JsonPropertyName("mandatory")] bool Mandatory)
{
    /// <summary>The version parsed for comparison.</summary>
    [JsonIgnore]
    public System.Version Parsed => System.Version.Parse(Version);
}
