using CheapHelpers.Helpers;
using Microsoft.Extensions.Options;

namespace CheapHelpers.Ota;

/// <summary>
/// Reads releases from a local folder laid out as <c>{LocalPath}/{version}/{asset}</c>.
/// An empty file named <c>mandatory</c> in a version folder marks that release as mandatory.
/// </summary>
public sealed class LocalFolderReleaseSource(IOptions<OtaOptions> options) : IOtaReleaseSource
{
    private const string MandatoryMarkerFile = "mandatory";

    private readonly OtaOptions _options = options.Value;

    /// <inheritdoc />
    /// <remarks>A missing folder gives an empty list.</remarks>
    public Task<IReadOnlyList<OtaRelease>> GetReleasesAsync(CancellationToken cancellationToken = default) =>
        Task.Run(ReadReleases, cancellationToken);

    /// <inheritdoc />
    /// <remarks>Only names that pass <see cref="OtaOptions.IsKnownAsset"/> are opened, and never outside the folder.</remarks>
    public Task<Stream?> OpenAssetAsync(string version, string assetName, CancellationToken cancellationToken = default) =>
        Task.Run(() => OpenAsset(version, assetName), cancellationToken);

    private IReadOnlyList<OtaRelease> ReadReleases()
    {
        var root = RootPath();
        if (!Directory.Exists(root))
            return [];

        return Directory.EnumerateDirectories(root)
            .Select(folder => (Folder: folder, Version: Path.GetFileName(folder)))
            .Where(entry => System.Version.TryParse(entry.Version, out _))
            .Select(entry => new OtaRelease(
                entry.Version,
                _options.TagPrefix + entry.Version,
                File.Exists(Path.Combine(entry.Folder, MandatoryMarkerFile))))
            .OrderByDescending(release => release.Parsed)
            .ToList();
    }

    private Stream? OpenAsset(string version, string assetName)
    {
        if (!System.Version.TryParse(version, out _) || !_options.IsKnownAsset(assetName))
            return null;

        var root = RootPath();
        var assetPath = Path.Combine(root, version, assetName);
        if (!PathHelpers.IsContainedIn(assetPath, root) || !File.Exists(assetPath))
            return null;

        return File.OpenRead(assetPath);
    }

    private string RootPath() =>
        _options.LocalPath ?? throw new InvalidOperationException($"{nameof(OtaOptions)}.{nameof(OtaOptions.LocalPath)} is required");
}
