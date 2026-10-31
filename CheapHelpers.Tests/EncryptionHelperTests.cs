using CheapHelpers.Helpers.Encryption;

namespace CheapHelpers.Tests;

/// <summary>Pins the key derivation and the round trips of both cipher modes.</summary>
public sealed class EncryptionHelperTests
{
    private const string MachineName = "machine";
    private const string UserName = "user";
    private const string PlainText = "route value 42";
    private const string Garbage = "bm90IGEgcmVhbCBjaXBoZXJ0ZXh0IGF0IGFsbA==";
    private const string ExpectedKeyHex = "A0BA1A772BE4FC045592A23B5CEAACC0D1C29BAB383C3C5811B6014EAE4A0E17";
    private const string ExpectedIvHex = "18D7C1725AFCEBBA5366587067897D9C";

    [Fact]
    public void Derivation_matches_the_pinned_vector()
    {
        var (key, iv) = EncryptionHelper.DeriveKeyAndIV(MachineName, UserName);

        Assert.Equal(32, key.Length);
        Assert.Equal(16, iv.Length);
        Assert.Equal(ExpectedKeyHex, Convert.ToHexString(key));
        Assert.Equal(ExpectedIvHex, Convert.ToHexString(iv));
    }

    [Fact]
    public void Same_inputs_give_the_same_key()
    {
        var first = EncryptionHelper.DeriveKeyAndIV(MachineName, UserName);
        var second = EncryptionHelper.DeriveKeyAndIV(MachineName, UserName);

        Assert.Equal(first.Key, second.Key);
        Assert.Equal(first.IV, second.IV);
    }

    [Fact]
    public void Different_user_or_machine_gives_a_different_key()
    {
        var (baseKey, _) = EncryptionHelper.DeriveKeyAndIV(MachineName, UserName);
        var (otherUserKey, _) = EncryptionHelper.DeriveKeyAndIV(MachineName, "other");
        var (otherMachineKey, _) = EncryptionHelper.DeriveKeyAndIV("other", UserName);

        Assert.NotEqual(baseKey, otherUserKey);
        Assert.NotEqual(baseKey, otherMachineKey);
    }

    [Fact]
    public void Separator_keeps_shifted_inputs_apart()
    {
        var (first, _) = EncryptionHelper.DeriveKeyAndIV("ab", "c");
        var (second, _) = EncryptionHelper.DeriveKeyAndIV("a", "bc");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Deterministic_encryption_repeats_and_round_trips()
    {
        var first = EncryptionHelper.EncryptDeterministic(PlainText);
        var second = EncryptionHelper.EncryptDeterministic(PlainText);

        Assert.Equal(first, second);
        Assert.Equal(PlainText, EncryptionHelper.DecryptDeterministic(first));
    }

    [Fact]
    public void Random_iv_encryption_differs_and_round_trips()
    {
        var first = EncryptionHelper.EncryptWithRandomIV(PlainText);
        var second = EncryptionHelper.EncryptWithRandomIV(PlainText);

        Assert.NotEqual(first, second);
        Assert.Equal(PlainText, EncryptionHelper.DecryptWithRandomIV(first));
        Assert.Equal(PlainText, EncryptionHelper.DecryptWithRandomIV(second));
    }

    [Fact]
    public void Decrypt_throws_on_garbage()
    {
        Assert.Throws<InvalidOperationException>(() => EncryptionHelper.DecryptDeterministic(Garbage));
        Assert.Throws<InvalidOperationException>(() => EncryptionHelper.DecryptWithRandomIV(Garbage));
    }

    [Fact]
    public void Empty_input_returns_empty()
    {
        Assert.Equal(string.Empty, EncryptionHelper.EncryptDeterministic(string.Empty));
        Assert.Equal(string.Empty, EncryptionHelper.DecryptDeterministic(string.Empty));
        Assert.Equal(string.Empty, EncryptionHelper.EncryptWithRandomIV(string.Empty));
        Assert.Equal(string.Empty, EncryptionHelper.DecryptWithRandomIV(string.Empty));
    }
}
