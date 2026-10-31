namespace CheapHelpers.Ota;

/// <summary>
/// Lists gateway releases and opens their assets.
/// </summary>
public interface IOtaReleaseSource
{
    /// <summary>Returns the releases newest first, only those whose tag carries the prefix and whose version parses.</summary>
    Task<IReadOnlyList<OtaRelease>> GetReleasesAsync(CancellationToken cancellationToken = default);

    /// <summary>Opens an asset of a release; null when the release or the asset does not exist.</summary>
    Task<Stream?> OpenAssetAsync(string version, string assetName, CancellationToken cancellationToken = default);
}
