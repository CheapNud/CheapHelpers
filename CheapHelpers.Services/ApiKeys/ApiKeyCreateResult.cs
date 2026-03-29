namespace CheapHelpers.Services.ApiKeys;

/// <summary>
/// Result of an API key generation or rotation operation.
/// <see cref="FullKey"/> is only available at creation time — store it immediately.
/// </summary>
public sealed record ApiKeyCreateResult(
    bool Success,
    int KeyId,
    string FullKey,
    string KeyPrefix,
    string? ErrorMessage = null)
{
    public static ApiKeyCreateResult Succeeded(int keyId, string fullKey, string keyPrefix) =>
        new(true, keyId, fullKey, keyPrefix);

    public static ApiKeyCreateResult Failed(string errorMessage) =>
        new(false, 0, string.Empty, string.Empty, errorMessage);
}
