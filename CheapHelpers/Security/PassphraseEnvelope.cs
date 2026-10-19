using System.Security.Cryptography;

namespace CheapHelpers.Security;

/// <summary>
/// Passphrase encryption for exports and backups: AES-256-GCM under a PBKDF2-HMAC-SHA256 key, built-in primitives only.
/// </summary>
/// <remarks>
/// Wire format: magic (optional, caller-chosen) | version (1 byte, 1) | salt (16) | nonce (12) | tag (16) | ciphertext,
/// with 600,000 PBKDF2 iterations and the passphrase encoded as UTF-8. This layout is frozen: files written by
/// earlier copies of this code must keep opening, so a change needs a new version byte, never an edit to version 1.
/// Pass the same magic to <see cref="Encrypt"/> and <see cref="Decrypt"/>; without one the version byte comes first.
/// </remarks>
public static class PassphraseEnvelope
{
    private const byte FormatVersion = 1;
    private const int SaltLength = 16;
    private const int NonceLength = 12;
    private const int TagLength = 16;
    private const int KeyLength = 32;
    private const int Iterations = 600_000;
    private const int HeaderLengthWithoutMagic = 1 + SaltLength + NonceLength + TagLength;

    /// <summary>Encrypts <paramref name="plaintext"/> with a key derived from <paramref name="passphrase"/> and a fresh random salt and nonce.</summary>
    /// <param name="plaintext">The bytes to protect.</param>
    /// <param name="passphrase">The passphrase; must not be empty.</param>
    /// <param name="magic">Optional file signature written before the version byte.</param>
    public static byte[] Encrypt(ReadOnlySpan<byte> plaintext, string passphrase, ReadOnlySpan<byte> magic = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(passphrase);

        var envelope = new byte[magic.Length + HeaderLengthWithoutMagic + plaintext.Length];
        var span = envelope.AsSpan();
        magic.CopyTo(span);

        var header = span[magic.Length..];
        header[0] = FormatVersion;
        var salt = header.Slice(1, SaltLength);
        var nonce = header.Slice(1 + SaltLength, NonceLength);
        var tag = header.Slice(1 + SaltLength + NonceLength, TagLength);
        RandomNumberGenerator.Fill(salt);
        RandomNumberGenerator.Fill(nonce);

        var key = DeriveKey(passphrase, salt);
        try
        {
            using var gcm = new AesGcm(key, TagLength);
            gcm.Encrypt(nonce, plaintext, header[HeaderLengthWithoutMagic..], tag);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }

        return envelope;
    }

    /// <summary>Decrypts an envelope written by <see cref="Encrypt"/> with the same passphrase and magic.</summary>
    /// <param name="envelope">The encrypted bytes.</param>
    /// <param name="passphrase">The passphrase used to encrypt.</param>
    /// <param name="magic">The file signature passed to <see cref="Encrypt"/>, if any.</param>
    /// <exception cref="CryptographicException">The data is not an envelope of this format, the passphrase is wrong, or the data was altered.</exception>
    public static byte[] Decrypt(ReadOnlySpan<byte> envelope, string passphrase, ReadOnlySpan<byte> magic = default)
    {
        ArgumentNullException.ThrowIfNull(passphrase);

        if (envelope.Length < magic.Length + HeaderLengthWithoutMagic || !envelope[..magic.Length].SequenceEqual(magic))
            throw new CryptographicException("The data is not a passphrase envelope.");

        var header = envelope[magic.Length..];
        if (header[0] != FormatVersion)
            throw new CryptographicException($"Unsupported passphrase envelope version {header[0]}.");

        var salt = header.Slice(1, SaltLength);
        var nonce = header.Slice(1 + SaltLength, NonceLength);
        var tag = header.Slice(1 + SaltLength + NonceLength, TagLength);
        var ciphertext = header[HeaderLengthWithoutMagic..];

        var plaintext = new byte[ciphertext.Length];
        var key = DeriveKey(passphrase, salt);
        try
        {
            using var gcm = new AesGcm(key, TagLength);
            // AuthenticationTagMismatchException (a CryptographicException) covers both a wrong passphrase and tampering.
            gcm.Decrypt(nonce, ciphertext, tag, plaintext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }

        return plaintext;
    }

    private static byte[] DeriveKey(string passphrase, ReadOnlySpan<byte> salt) =>
        Rfc2898DeriveBytes.Pbkdf2(passphrase, salt, Iterations, HashAlgorithmName.SHA256, KeyLength);
}
