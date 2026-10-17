using System.Security.Cryptography;
using System.Text;

namespace CheapHelpers.Services.Voice;

/// <summary>
/// Computes the <c>Sec-MS-GEC</c> challenge that Microsoft's Edge TTS endpoint
/// requires on the WebSocket handshake. The TrustedClientToken alone stopped being
/// enough sometime in 2024–2025; without this header the upgrade returns HTTP 403
/// and synthesis fails before a single byte is produced.
/// </summary>
/// <remarks>
/// Algorithm (ported from the open-source <c>edge-tts</c> Python and Go clients):
/// <list type="number">
///   <item>Take the current UTC time as a Windows FILETIME (100-nanosecond intervals
///         since 1601-01-01 UTC).</item>
///   <item>Round it down to the nearest 5-minute bucket (= 3_000_000_000 ticks).</item>
///   <item>Concatenate the decimal bucket value with <see cref="EdgeTextToSpeech.TrustedClientToken"/>
///         (ASCII).</item>
///   <item>SHA-256 hash the bytes and return the digest as upper-case hex.</item>
/// </list>
/// Rounding to 5-minute buckets means many requests share the same hash, which is
/// what makes the scheme cacheable on the server. It also gives us a stable input
/// to test against — pin a timestamp inside a bucket and the output is deterministic.
/// </remarks>
public static class EdgeTtsSecurity
{
    private const long FiveMinuteTicks = 3_000_000_000L; // 5 min × 60 s × 10_000_000 (100ns ticks)

    /// <summary>
    /// Chromium version we claim to be — paired between <c>SecMsGecVersion</c> below
    /// and the <c>User-Agent</c> string. Microsoft's gateway sniffs both; if they
    /// disagree (e.g. UA says 130, version header says 143) or either is stale, the
    /// handshake returns 403. Tracks whatever the upstream <c>rany2/edge-tts</c>
    /// Python package pins in its constants — bump this when that does.
    /// </summary>
    public const string ChromiumFullVersion = "143.0.3650.75";
    public const string ChromiumMajorVersion = "143";

    /// <summary>
    /// Browser version sent alongside the Sec-MS-GEC token. Format <c>"1-{full}"</c>
    /// to match the upstream package.
    /// </summary>
    public const string SecMsGecVersion = "1-" + ChromiumFullVersion;

    /// <summary>
    /// Returns the upper-case hex SHA-256 of the 5-minute FILETIME bucket of <paramref name="utcNow"/> followed by <paramref name="trustedClientToken"/>.
    /// </summary>
    public static string ComputeSecMsGec(DateTimeOffset utcNow, string trustedClientToken)
    {
        // DateTime.ToFileTimeUtc maps to the FILETIME contract precisely (100ns ticks
        // since 1601-01-01 UTC), which is what the upstream reference uses.
        var fileTime = utcNow.UtcDateTime.ToFileTimeUtc();
        var bucket = fileTime - (fileTime % FiveMinuteTicks);

        var input = $"{bucket}{trustedClientToken}";
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(input));
        return Convert.ToHexString(hash); // Convert.ToHexString returns upper-case by spec
    }
}
