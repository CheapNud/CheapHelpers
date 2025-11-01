using System;

namespace CheapHelpers.Extensions
{
    public static class UriExtensions
    {
        /// <summary>
        /// Extracts the base URL (scheme + authority) from a full URL string.
        /// Supports any URI scheme (http, https, ftp, etc.) and preserves port numbers.
        /// </summary>
        /// <param name="url">The full URL string</param>
        /// <returns>The base URL (e.g., "https://example.com")</returns>
        /// <exception cref="ArgumentException">Thrown when the URL format is invalid</exception>
        /// <example>
        /// "https://example.com/path/to/page".GetUrlBase() returns "https://example.com"
        /// "http://example.com:8080/api/endpoint".GetUrlBase() returns "http://example.com:8080"
        /// </example>
        public static string GetUrlBase(this string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            {
                throw new ArgumentException("Invalid URL format.", nameof(url));
            }

            return $"{uri.Scheme}://{uri.Authority}";
        }
    }
}
