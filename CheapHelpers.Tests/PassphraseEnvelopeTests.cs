using System.Security.Cryptography;
using System.Text;
using CheapHelpers.Security;

namespace CheapHelpers.Tests;

/// <summary>Pins the round trip, the failure modes and the frozen wire format.</summary>
public sealed class PassphraseEnvelopeTests
{
    private const string Passphrase = "correct horse battery staple";
    private static readonly byte[] Plaintext = Encoding.UTF8.GetBytes("settings backup payload");

    [Fact]
    public void Round_trip_returns_the_plaintext()
    {
        var envelope = PassphraseEnvelope.Encrypt(Plaintext, Passphrase);

        Assert.Equal(1, envelope[0]);
        Assert.Equal(1 + 16 + 12 + 16 + Plaintext.Length, envelope.Length);
        Assert.Equal(Plaintext, PassphraseEnvelope.Decrypt(envelope, Passphrase));
    }

    [Fact]
    public void Wrong_passphrase_throws()
    {
        var envelope = PassphraseEnvelope.Encrypt(Plaintext, Passphrase);

        Assert.ThrowsAny<CryptographicException>(() => PassphraseEnvelope.Decrypt(envelope, "wrong"));
    }

    [Fact]
    public void Tampered_ciphertext_throws()
    {
        var envelope = PassphraseEnvelope.Encrypt(Plaintext, Passphrase);
        envelope[^1] ^= 0x01;

        Assert.ThrowsAny<CryptographicException>(() => PassphraseEnvelope.Decrypt(envelope, Passphrase));
    }

    [Fact]
    public void Wrong_magic_throws()
    {
        var envelope = PassphraseEnvelope.Encrypt(Plaintext, Passphrase, "ABCD"u8);

        Assert.ThrowsAny<CryptographicException>(() => PassphraseEnvelope.Decrypt(envelope, Passphrase, "WXYZ"u8));
    }

    [Fact]
    public void Opens_an_envelope_built_by_hand_from_the_frozen_layout()
    {
        // magic(4) | version(1) | salt(16) | nonce(12) | tag(16) | ciphertext, PBKDF2-SHA256 at 600,000 iterations, 16-byte tag.
        var magic = "CVBK"u8.ToArray();
        var salt = RandomNumberGenerator.GetBytes(16);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var key = Rfc2898DeriveBytes.Pbkdf2(Passphrase, salt, 600_000, HashAlgorithmName.SHA256, 32);
        var ciphertext = new byte[Plaintext.Length];
        var tag = new byte[16];
        using (var gcm = new AesGcm(key, 16))
            gcm.Encrypt(nonce, Plaintext, ciphertext, tag);

        byte[] envelope = [.. magic, 1, .. salt, .. nonce, .. tag, .. ciphertext];

        Assert.Equal(Plaintext, PassphraseEnvelope.Decrypt(envelope, Passphrase, magic));
    }
}
