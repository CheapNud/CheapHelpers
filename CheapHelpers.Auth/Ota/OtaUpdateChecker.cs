using CheapHelpers.Ota;
using Microsoft.Extensions.Options;

namespace CheapHelpers.Auth.Ota;

/// <summary>
/// Builds the update answer for a gateway from the release source and <see cref="OtaUpdatePolicy"/>.
/// The app's device endpoint calls it after it has authenticated the device and loaded the device's pin.
/// </summary>
public sealed class OtaUpdateChecker(IOtaReleaseSource source, IOptions<OtaOptions> options)
{
    private const string ReleasesSegment = "releases";

    private readonly OtaOptions _options = options.Value;

    /// <summary>
    /// Returns the release the device should install with its download and checksum paths under <paramref name="routePrefix"/>,
    /// or null when nothing newer applies.
    /// </summary>
    /// <exception cref="ArgumentException">The architecture is not in <see cref="OtaOptions.Assets"/>.</exception>
    public async Task<OtaUpdateCheck?> CheckAsync(
        string? currentVersion,
        string architecture,
        string? devicePin,
        string routePrefix,
        CancellationToken cancellationToken = default)
    {
        var assetName = RequireAsset(architecture);
        var releases = await source.GetReleasesAsync(cancellationToken);
        var target = OtaUpdatePolicy.ResolveTarget(currentVersion, devicePin, _options.InstancePin, releases);

        return target is null ? null : Build(target, assetName, OtaUpdatePolicy.IsMandatoryAbove(currentVersion, releases), routePrefix);
    }

    /// <summary>The newest release regardless of pins, or null when there is none.</summary>
    /// <exception cref="ArgumentException">The architecture is not in <see cref="OtaOptions.Assets"/>.</exception>
    internal async Task<OtaUpdateCheck?> LatestAsync(string? since, string architecture, string routePrefix, CancellationToken cancellationToken)
    {
        var assetName = RequireAsset(architecture);
        var releases = await source.GetReleasesAsync(cancellationToken);
        var newest = releases.MaxBy(release => release.Parsed);

        return newest is null ? null : Build(newest, assetName, OtaUpdatePolicy.IsMandatoryAbove(since, releases), routePrefix);
    }

    private string RequireAsset(string architecture) =>
        _options.AssetFor(architecture)
        ?? throw new ArgumentException($"Unknown architecture '{architecture}'", nameof(architecture));

    private OtaUpdateCheck Build(OtaRelease release, string assetName, bool mandatory, string routePrefix)
    {
        var downloadUrl = $"{routePrefix.TrimEnd('/')}/{ReleasesSegment}/{Uri.EscapeDataString(release.Version)}/{Uri.EscapeDataString(assetName)}";
        return new OtaUpdateCheck(release.Version, release.Tag, mandatory, downloadUrl, downloadUrl + Uri.EscapeDataString(_options.ChecksumSuffix));
    }
}
