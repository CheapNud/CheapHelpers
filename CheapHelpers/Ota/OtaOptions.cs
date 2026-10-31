namespace CheapHelpers.Ota;

/// <summary>
/// Settings for the gateway update server.
/// </summary>
public class OtaOptions
{
    private const string DefaultTagPrefix = "v";
    private const string DefaultMandatoryMarker = "[mandatory]";
    private const string DefaultChecksumSuffix = ".sha256";
    private const int DefaultForgePageSize = 50;
    private const int DefaultForgeMaxPages = 50;
    private static readonly TimeSpan DefaultReleaseCacheDuration = TimeSpan.FromMinutes(5);

    /// <summary>Prefix in front of the version in a release tag.</summary>
    public string TagPrefix { get; set; } = DefaultTagPrefix;

    /// <summary>Text in a release body that marks the release as mandatory, matched case-insensitively.</summary>
    public string MandatoryMarker { get; set; } = DefaultMandatoryMarker;

    /// <summary>Architecture key (for example <c>armv7</c>) to the name of its binary asset (for example <c>agent-armv7</c>).</summary>
    public Dictionary<string, string> Assets { get; set; } = new();

    /// <summary>Suffix of the checksum asset that sits next to each binary.</summary>
    public string ChecksumSuffix { get; set; } = DefaultChecksumSuffix;

    /// <summary>Version every device without its own pin stays on; null follows the newest release.</summary>
    public string? InstancePin { get; set; }

    /// <summary>Where releases come from.</summary>
    public OtaSourceKind Source { get; set; } = OtaSourceKind.Forge;

    /// <summary>Repository API URL for <see cref="OtaSourceKind.Forge"/>, for example <c>https://forge.example/api/v1/repos/owner/repo</c>.</summary>
    public string? ForgeRepoApiUrl { get; set; }

    /// <summary>Access token for the forge; null for a public repository.</summary>
    public string? ForgeToken { get; set; }

    /// <summary>Release endpoint prefix of another instance for <see cref="OtaSourceKind.Upstream"/>, for example <c>https://other.example/api/ota</c>.</summary>
    public string? UpstreamUrl { get; set; }

    /// <summary>Folder for <see cref="OtaSourceKind.LocalFolder"/>, laid out as <c>{version}/{asset}</c>.</summary>
    public string? LocalPath { get; set; }

    /// <summary>How long the forge release list is cached.</summary>
    public TimeSpan ReleaseCacheDuration { get; set; } = DefaultReleaseCacheDuration;

    /// <summary>Releases per forge page.</summary>
    public int ForgePageSize { get; set; } = DefaultForgePageSize;

    /// <summary>Most forge pages read for one release list.</summary>
    public int ForgeMaxPages { get; set; } = DefaultForgeMaxPages;

    /// <summary>Throws when <see cref="Assets"/> is empty or the setting the chosen <see cref="Source"/> needs is blank.</summary>
    /// <exception cref="InvalidOperationException">Names the missing setting.</exception>
    public void Validate()
    {
        if (Assets.Count == 0)
            throw Missing(nameof(Assets));

        var (settingName, settingValue) = Source switch
        {
            OtaSourceKind.Forge => (nameof(ForgeRepoApiUrl), ForgeRepoApiUrl),
            OtaSourceKind.Upstream => (nameof(UpstreamUrl), UpstreamUrl),
            OtaSourceKind.LocalFolder => (nameof(LocalPath), LocalPath),
            _ => throw new InvalidOperationException($"{nameof(OtaOptions)}.{nameof(Source)} has an unknown value {Source}"),
        };

        if (string.IsNullOrWhiteSpace(settingValue))
            throw Missing(settingName);
    }

    /// <summary>True for every binary asset in <see cref="Assets"/> and for its checksum asset; nothing else may be served.</summary>
    public bool IsKnownAsset(string assetName) =>
        Assets.Values.Any(binary => assetName == binary || assetName == binary + ChecksumSuffix);

    /// <summary>The binary asset for an architecture key, matched case-insensitively; null when the architecture is unknown.</summary>
    public string? AssetFor(string architecture) =>
        Assets.FirstOrDefault(entry => string.Equals(entry.Key, architecture, StringComparison.OrdinalIgnoreCase)).Value;

    private static InvalidOperationException Missing(string settingName) =>
        new($"{nameof(OtaOptions)}.{settingName} is required");
}
