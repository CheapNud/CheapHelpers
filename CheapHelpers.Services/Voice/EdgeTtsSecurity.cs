using System.Buffers.Binary;
using System.Numerics;
using System.Text;

namespace CheapHelpers.Services.Voice;

/// <summary>
/// Computes the <c>Sec-MS-GEC</c> challenge that Microsoft's Edge TTS endpoint
/// requires on the WebSocket handshake. Without a valid value the upgrade returns
/// HTTP 403 and synthesis fails before a single byte is produced.
/// </summary>
/// <remarks>
/// Ported from the open-source <c>rany2/edge-tts</c> Python client, version 7.3.1:
/// <c>src/edge_tts/drm.py</c> at commit b2ffbdd9f43f28f03cc939b5de0f3964aa62d60e
/// (<c>generate_sec_ms_gec_v2</c>) and <c>src/edge_tts/constants.py</c> at commit
/// ba61d94442c18893f0c4d3c5ad253718b21ab93e (<c>SEC_MS_GEC_VERSION</c>). When Microsoft
/// changes the scheme again, diff those files against the commits named here.
/// <para>
/// Algorithm (the "v2" token, <c>&lt;16 hex&gt;-&lt;64 hex&gt;</c>):
/// <list type="number">
///   <item>Take the UTC time as a Windows FILETIME (100-nanosecond intervals since
///         1601-01-01 UTC) and round it down to a 5-minute bucket (= 3_000_000_000 ticks).</item>
///   <item>Concatenate the decimal bucket value with the TrustedClientToken (ASCII).</item>
///   <item>Hash it with upstream's modified SHA-256 (non-standard state and constants
///         for the first seven bytes, see <see cref="Sha256Digest"/>).</item>
///   <item>Feed that digest as the key into upstream's CS64 digest of the same bytes.</item>
///   <item>Return <c>CS64-SHA256</c>, both as upper-case hex.</item>
/// </list>
/// Rounding to 5-minute buckets means many requests share the same value, which is
/// what makes the scheme cacheable on the server. It also gives us a stable input
/// to test against — pin a timestamp inside a bucket and the output is deterministic.
/// </para>
/// </remarks>
public static class EdgeTtsSecurity
{
    private const long FiveMinuteTicks = 3_000_000_000L; // 5 min × 60 s × 10_000_000 (100ns ticks)

    /// <summary>
    /// Chromium version we claim to be — paired between <c>SecMsGecVersion</c> below
    /// and the <c>User-Agent</c> string. Microsoft's gateway sniffs both; if they
    /// disagree (e.g. UA says 130, version header says 143) or either is stale, the
    /// handshake returns 403. Tracks <c>CHROMIUM_FULL_VERSION</c> in upstream
    /// <c>rany2/edge-tts</c> constants.py — bump this when that does.
    /// </summary>
    public const string ChromiumFullVersion = "143.0.3650.75";
    public const string ChromiumMajorVersion = "143";

    /// <summary>
    /// Browser version sent alongside the Sec-MS-GEC token. Format <c>"2-{full}"</c>
    /// to match upstream's <c>SEC_MS_GEC_VERSION</c>; the "2-" prefix names the v2 token format.
    /// </summary>
    public const string SecMsGecVersion = "2-" + ChromiumFullVersion;

    // ---- Constants copied verbatim from upstream drm.py ----

    // FIPS 180-4 initial hash value; added to the custom state after the first block.
    private static readonly uint[] Sha256Initial =
    [
        0x6A09E667, 0xBB67AE85, 0x3C6EF372, 0xA54FF53A, 0x510E527F, 0x9B05688C, 0x1F83D9AB, 0x5BE0CD19,
    ];

    // Upstream _SHA256_STATE: working registers at round 14 of the first block.
    private static readonly uint[] Sha256State =
    [
        0xFD9FBEA2, 0x91243CE7, 0xBE499094, 0xCD2E1E0A, 0x6C006F49, 0x04BD35AB, 0xFEB0A7E4, 0x03EFFBD2,
    ];

    // Upstream _SHA256_SCHEDULE: precomputed message-schedule parts for words 16..29.
    private static readonly uint[] Sha256Schedule =
    [
        0xEB24BF9E, 0x1558691F, 0xA0C09EE2, 0x0CFA32BA, 0xA622EE8C, 0xD1DD9491, 0xC0DD25A0,
        0xF9E70AB6, 0xF0C31573, 0x7058CE4E, 0x7188E606, 0x00E5AB33, 0xD258B060, 0x72766564,
    ];

    private static readonly uint[] Sha256RoundConstants =
    [
        0x428A2F98, 0x71374491, 0xB5C0FBCF, 0xE9B5DBA5, 0x3956C25B, 0x59F111F1, 0x923F82A4, 0xAB1C5ED5,
        0xD807AA98, 0x12835B01, 0x243185BE, 0x550C7DC3, 0x72BE5D74, 0x80DEB1FE, 0x9BDC06A7, 0xC19BF174,
        0xE49B69C1, 0xEFBE4786, 0x0FC19DC6, 0x240CA1CC, 0x2DE92C6F, 0x4A7484AA, 0x5CB0A9DC, 0x76F988DA,
        0x983E5152, 0xA831C66D, 0xB00327C8, 0xBF597FC7, 0xC6E00BF3, 0xD5A79147, 0x06CA6351, 0x14292967,
        0x27B70A85, 0x2E1B2138, 0x4D2C6DFC, 0x53380D13, 0x650A7354, 0x766A0ABB, 0x81C2C92E, 0x92722C85,
        0xA2BFE8A1, 0xA81A664B, 0xC24B8B70, 0xC76C51A3, 0xD192E819, 0xD6990624, 0xF40E3585, 0x106AA070,
        0x19A4C116, 0x1E376C08, 0x2748774C, 0x34B0BCB5, 0x391C0CB3, 0x4ED8AA4A, 0x5B9CCA4F, 0x682E6FF3,
        0x748F82EE, 0x78A5636F, 0x84C87814, 0x8CC70208, 0x90BEFFFA, 0xA4506CEB, 0xBEF9A3F7, 0xC67178F2,
    ];

    private const uint Sha256Adjustment = 0xC9DC06A7;
    private const uint Sha256Sigma0Adjustment = 0x059C0B80;
    private const uint Sha256Sigma1Adjustment = 0x000B92C0;

    // Upstream _SHA256_BLOCK_CONSTANTS: the round constants with K[14] replaced by the adjustment.
    private static readonly uint[] Sha256BlockConstants =
        [.. Sha256RoundConstants[..14], Sha256Adjustment, .. Sha256RoundConstants[15..]];

    // Upstream _CS64_COEFFICIENTS as (coefficient, offset) pairs.
    private static readonly (uint Coefficient, uint Offset)[] Cs64Coefficients =
    [
        (0xE82E91B1, 0xB32700D0), (0x44DE24C6, 0x88DFF51D), (0x3BEB65BC, 0x29514038), (0xC89C5933, 0xB328CBDF),
        (0x8D1EC211, 0xD405B4D5), (0x314EF5D5, 0x26E55F3C), (0xE92491B1, 0x607801C6), (0xF0A5B9D5, 0x82B2C1FB),
        (0x06064CF3, 0xFA51A3DA), (0xDA79FA60, 0x207CC464), (0xBED20560, 0x3C3C74F3), (0x89F9C9F8, 0xD2D14B61),
        (0x35FA38C6, 0xBB04A2C1), (0xCB49E3C6, 0xC5626167),
    ];

    private const uint Cs64Factor = 0x82893933;
    private const uint Cs64Adjustment0 = 0xF2F90B1A;
    private const uint Cs64Adjustment1 = 0x0B1A0000;

    /// <summary>
    /// Returns the v2 token (<c>&lt;16 hex&gt;-&lt;64 hex&gt;</c>) for the 5-minute FILETIME bucket of
    /// <paramref name="utcNow"/> and <paramref name="trustedClientToken"/>. Upstream: <c>generate_sec_ms_gec_v2</c>.
    /// </summary>
    public static string ComputeSecMsGec(DateTimeOffset utcNow, string trustedClientToken)
    {
        // DateTime.ToFileTimeUtc maps to the FILETIME contract precisely (100ns ticks
        // since 1601-01-01 UTC). Upstream does the same in seconds (WIN_EPOCH, % 300, × 1e7);
        // both land on the same integer.
        var fileTime = utcNow.UtcDateTime.ToFileTimeUtc();
        var bucket = fileTime - (fileTime % FiveMinuteTicks);

        var input = Encoding.ASCII.GetBytes($"{bucket}{trustedClientToken}");
        var digest = Sha256Digest(input);
        var (cs64Low, cs64High) = Cs64Digest(input, digest);

        Span<byte> cs64 = stackalloc byte[8];
        BinaryPrimitives.WriteUInt32LittleEndian(cs64, cs64Low);
        BinaryPrimitives.WriteUInt32LittleEndian(cs64[4..], cs64High);
        return $"{Convert.ToHexString(cs64)}-{Convert.ToHexString(digest)}";
    }

    private static uint Rol16(uint value) => BitOperations.RotateLeft(value, 16);

    // Standard SHA-256 small sigmas (upstream _sigma0 / _sigma1).
    private static uint Sigma0(uint value) =>
        BitOperations.RotateRight(value, 7) ^ BitOperations.RotateRight(value, 18) ^ (value >> 3);

    private static uint Sigma1(uint value) =>
        BitOperations.RotateRight(value, 17) ^ BitOperations.RotateRight(value, 19) ^ (value >> 10);

    /// <summary>Upstream _expand_sha256_schedule: fills words[firstWord..63].</summary>
    private static void ExpandSchedule(uint[] words, int firstWord)
    {
        for (var index = firstWord; index < 64; index++)
            words[index] = Sigma1(words[index - 2]) + words[index - 7] + Sigma0(words[index - 15]) + words[index - 16];
    }

    /// <summary>
    /// Upstream _sha256_rounds: standard SHA-256 rounds from <paramref name="firstRound"/> to 63.
    /// Upstream unrolls this for speed; the arithmetic is the same. Resuming at round 14 uses
    /// the block constants with the replaced K[14]. Returns the working registers (not yet added to the state).
    /// </summary>
    private static uint[] Rounds(uint[] state, uint[] words, int firstRound)
    {
        var constants = firstRound == 14 ? Sha256BlockConstants : Sha256RoundConstants;
        uint a = state[0], b = state[1], c = state[2], d = state[3], e = state[4], f = state[5], g = state[6], h = state[7];

        for (var index = firstRound; index < 64; index++)
        {
            var t1 = h
                + (BitOperations.RotateRight(e, 6) ^ BitOperations.RotateRight(e, 11) ^ BitOperations.RotateRight(e, 25))
                + (g ^ (e & (f ^ g)))
                + constants[index]
                + words[index];
            var t2 = (BitOperations.RotateRight(a, 2) ^ BitOperations.RotateRight(a, 13) ^ BitOperations.RotateRight(a, 22))
                + ((a & b) | (c & (a | b)));
            h = g; g = f; f = e; e = d + t1; d = c; c = b; b = a; a = t1 + t2;
        }

        return [a, b, c, d, e, f, g, h];
    }

    /// <summary>Upstream _sha256_init: the custom state after the first seven input bytes.</summary>
    private static uint[] Sha256Init(ReadOnlySpan<byte> firstSevenBytes)
    {
        var words = new uint[64];
        words[14] = (uint)((firstSevenBytes[0] << 16) | (firstSevenBytes[1] << 8) | firstSevenBytes[2]);
        words[15] = BinaryPrimitives.ReadUInt32BigEndian(firstSevenBytes[3..7]);

        for (var index = 16; index < 30; index++)
        {
            var value = Sha256Schedule[index - 16];
            value += index == 16 ? Sigma1(words[index - 2]) ^ Sha256Sigma1Adjustment : Sigma1(words[index - 2]);
            if (index - 7 >= 14)
                value += words[index - 7];
            if (index - 15 >= 14)
                value += index == 29 ? Sigma0(words[index - 15]) ^ Sha256Sigma0Adjustment : Sigma0(words[index - 15]);
            words[index] = value;
        }

        words[30] = Sigma1(words[28]) + words[23] + Sigma0(words[15]) + words[14] + Sha256Adjustment - Sha256RoundConstants[14];
        ExpandSchedule(words, 31);

        var working = Rounds(Sha256State, words, 14);
        return [.. Sha256Initial.Select((initial, i) => initial + working[i])];
    }

    /// <summary>Upstream _sha256_digest: the modified SHA-256 of the token input (at least seven bytes).</summary>
    private static byte[] Sha256Digest(byte[] input)
    {
        if (input.Length < 7)
            throw new ArgumentException("Token salt must contain at least seven bytes", nameof(input));

        // Upstream counts the first seven bytes as if they filled a 64-byte block (57 + 7 = 64).
        var length = 57 + input.Length;
        var zeroCount = ((55 - length) % 64 + 64) % 64;
        var padded = new byte[input.Length - 7 + 1 + zeroCount + 8];
        input.AsSpan(7).CopyTo(padded);
        padded[input.Length - 7] = 0x80;
        BinaryPrimitives.WriteUInt64BigEndian(padded.AsSpan(padded.Length - 8), (ulong)length * 8);

        var state = Sha256Init(input.AsSpan(0, 7));
        var words = new uint[64];
        for (var offset = 0; offset < padded.Length; offset += 64)
        {
            for (var i = 0; i < 16; i++)
                words[i] = BinaryPrimitives.ReadUInt32BigEndian(padded.AsSpan(offset + i * 4, 4));
            ExpandSchedule(words, 16);
            var working = Rounds(state, words, 0);
            for (var i = 0; i < 8; i++)
                state[i] += working[i];
        }

        var digest = new byte[32];
        for (var i = 0; i < 8; i++)
            BinaryPrimitives.WriteUInt32BigEndian(digest.AsSpan(i * 4), state[i]);
        return digest;
    }

    /// <summary>Upstream _cs64_digest: two 32-bit words keyed by the first eight digest bytes.</summary>
    private static (uint Low, uint High) Cs64Digest(byte[] input, byte[] digest)
    {
        var keyA = BinaryPrimitives.ReadUInt32LittleEndian(digest) | 1;
        var keyB = BinaryPrimitives.ReadUInt32LittleEndian(digest.AsSpan(4)) | 1;
        uint[] factors = [keyA * Cs64Factor, keyB * Cs64Factor];

        List<uint> terms = [.. Cs64Coefficients.Select((pair, i) => pair.Coefficient * factors[i % 2])];
        List<uint> offsets = [.. Cs64Coefficients.Select(pair => pair.Offset)];

        // Upstream _words_le4_even(b"\x00" + data): little-endian words, zero-padded to an even count.
        var prefixed = new byte[1 + input.Length];
        input.CopyTo(prefixed, 1);
        var wordCount = (prefixed.Length + 3) / 4;
        wordCount += wordCount % 2;
        var wordBytes = new byte[wordCount * 4];
        prefixed.CopyTo(wordBytes, 0);

        for (var index = 0; index < wordCount; index++)
        {
            var word = BinaryPrimitives.ReadUInt32LittleEndian(wordBytes.AsSpan(index * 4));
            var term = word * (index % 2 == 0 ? keyA : keyB);
            var offset = Rol16(word) * (index % 2 == 0 ? 0xEF0569FBu : 0xC31713DBu);
            if (index == 0)
            {
                term += Cs64Adjustment0 * factors[0];
                offset += Cs64Adjustment1;
            }
            terms.Add(term);
            offsets.Add(offset);
        }

        uint swapAcc = 0, swapLast = 0, state = 0, acc = 0;
        for (var index = 0; index < terms.Count; index += 2)
        {
            uint x = terms[index], y = terms[index + 1];

            var q = Rol16(offsets[index] + x) * 0x76F50F34;
            swapAcc += Rol16(q) * 0xC3EFEA97 + q;
            q = Rol16(offsets[index + 1] + y) * 0x1881CE3C;
            swapLast = Rol16(q) * 0x35BD1EC9 + q;
            swapAcc += swapLast;

            state = state * keyA + x;
            state = Rol16(state) * 0xCF98B111;
            state = Rol16(state) * 0x87085B9F;
            state = Rol16(state) * 0x12CEB96D;
            state = Rol16(state) * 0x257E1D83;
            acc += state;
            var t = state * keyB + y;
            t = Rol16(t) * 0xA27416F5;
            t = Rol16(t) * 0xD38396FF;
            state = Rol16(Rol16(t) * 0x7C932B89) * 0xBFA49F69;
            acc += state;
        }

        return (swapLast ^ state, swapAcc ^ acc);
    }
}
