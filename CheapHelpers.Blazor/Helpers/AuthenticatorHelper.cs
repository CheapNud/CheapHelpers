using System.Text;
using System.Text.Encodings.Web;

namespace CheapHelpers.Blazor.Helpers;

/// <summary>
/// Shared helper for TOTP authenticator key formatting and QR URI generation.
/// Used by both Authenticator.razor and AccountController.
/// </summary>
public static class AuthenticatorHelper
{
    private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";
    private const int KeyGroupSize = 4;

    /// <summary>
    /// Formats an authenticator key into groups of 4 characters for readability.
    /// </summary>
    public static string FormatKey(string unformattedKey)
    {
        var sb = new StringBuilder();
        int currentPosition = 0;

        while (currentPosition + KeyGroupSize < unformattedKey.Length)
        {
            sb.Append(unformattedKey.AsSpan(currentPosition, KeyGroupSize)).Append(' ');
            currentPosition += KeyGroupSize;
        }

        if (currentPosition < unformattedKey.Length)
        {
            sb.Append(unformattedKey.AsSpan(currentPosition));
        }

        return sb.ToString().ToLowerInvariant();
    }

    /// <summary>
    /// Generates the otpauth:// URI used to create QR codes for authenticator app enrollment.
    /// </summary>
    public static string GenerateQrCodeUri(string appName, string email, string unformattedKey) =>
        string.Format(
            AuthenticatorUriFormat,
            UrlEncoder.Default.Encode(appName),
            UrlEncoder.Default.Encode(email),
            unformattedKey);
}
