using System.Text.Json.Serialization;

namespace CheapHelpers.Ota;

/// <summary>
/// The answer to an update check, as sent to a gateway.
/// </summary>
/// <param name="Version">The version to install, without the tag prefix.</param>
/// <param name="Tag">The full release tag.</param>
/// <param name="Mandatory">True when a release above the device's current version is mandatory.</param>
/// <param name="DownloadUrl">Relative path of the binary for the device's architecture.</param>
/// <param name="Sha256Url">Relative path of the binary's <c>sha256sum</c> sidecar.</param>
public sealed record OtaUpdateCheck(
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("tag")] string Tag,
    [property: JsonPropertyName("mandatory")] bool Mandatory,
    [property: JsonPropertyName("download_url")] string DownloadUrl,
    [property: JsonPropertyName("sha256_url")] string Sha256Url);
