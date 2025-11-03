using Foundation;
using System.Text;

namespace CheapHelpers.MAUI.Platforms.iOS;

/// <summary>
/// Extension methods for NSData to convert APNS tokens to hex strings
/// </summary>
internal static class NSDataExtensions
{
    /// <summary>
    /// Convert NSData (APNS token) to hex string format
    /// </summary>
    internal static string ToHexString(this NSData data)
    {
        var bytes = data.ToArray();
        if (bytes == null)
            return string.Empty;

        var sb = new StringBuilder(bytes.Length * 2);
        foreach (byte b in bytes)
            sb.AppendFormat("{0:x2}", b);

        return sb.ToString().ToUpperInvariant();
    }
}
