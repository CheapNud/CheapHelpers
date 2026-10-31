namespace CheapHelpers.Ota;

/// <summary>
/// Decides which release a gateway should install.
/// </summary>
public static class OtaUpdatePolicy
{
    private const char VersionPrefix = 'v';
    private static readonly Version Zero = new(0, 0, 0);

    /// <summary>
    /// Returns the release a device should move to, or null when it should stay.
    /// The pin is the device pin, else the instance pin. With a pin the candidate is the release with that version (none when
    /// no release matches); without one it is the newest release. A mandatory release above the candidate replaces it.
    /// The candidate is returned only when it is above the current version; an unparsable current version counts as 0.0.0.
    /// </summary>
    public static OtaRelease? ResolveTarget(string? currentVersion, string? devicePin, string? instancePin, IReadOnlyList<OtaRelease> releases)
    {
        var pin = string.IsNullOrWhiteSpace(devicePin) ? instancePin : devicePin;

        OtaRelease? candidate;
        if (string.IsNullOrWhiteSpace(pin))
            candidate = releases.MaxBy(release => release.Parsed);
        else
            candidate = TryParse(pin, out var pinned) ? releases.FirstOrDefault(release => release.Parsed == pinned) : null;

        var newestMandatory = releases.Where(release => release.Mandatory).MaxBy(release => release.Parsed);
        if (newestMandatory is not null && (candidate is null || newestMandatory.Parsed > candidate.Parsed))
            candidate = newestMandatory;

        var current = TryParse(currentVersion, out var parsed) ? parsed : Zero;
        return candidate is not null && candidate.Parsed > current ? candidate : null;
    }

    /// <summary>
    /// True when any release above the current version is mandatory.
    /// With no parsable current version only the newest release's own flag counts.
    /// </summary>
    public static bool IsMandatoryAbove(string? currentVersion, IReadOnlyList<OtaRelease> releases)
    {
        if (!TryParse(currentVersion, out var current))
            return releases.MaxBy(release => release.Parsed)?.Mandatory ?? false;

        return releases.Any(release => release.Mandatory && release.Parsed > current);
    }

    /// <summary>Parses a version after trimming one leading <c>v</c>.</summary>
    internal static bool TryParse(string? text, out Version version)
    {
        var trimmed = text?.Trim() ?? string.Empty;
        if (trimmed.StartsWith(VersionPrefix))
            trimmed = trimmed[1..];

        if (Version.TryParse(trimmed, out var parsed))
        {
            version = parsed;
            return true;
        }

        version = Zero;
        return false;
    }
}
