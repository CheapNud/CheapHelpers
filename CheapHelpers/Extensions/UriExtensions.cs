using System;

namespace CheapHelpers.Extensions
{
    public static class UriExtensions
    {
        /// <summary>
        /// Extracts the base URL (scheme + host) from a full URL string.
        /// Handles both http and https protocols.
        /// </summary>
        /// <param name="url">The full URL string</param>
        /// <returns>The base URL (e.g., "https://example.com")</returns>
        /// <example>
        /// "https://example.com/path/to/page".GetUrlBase() returns "https://example.com"
        /// "http://example.com:8080/api/endpoint".GetUrlBase() returns "http://example.com:8080"
        /// </example>
        public static string GetUrlBase(this string url)
        {
            bool isHttps = false;

            if (url.Contains("https://"))
            {
                isHttps = true;
                url = url.Replace("https://", "").Split('/')[0];
            }
            else if (url.Contains("http://"))
            {
                url = url.Replace("http://", "").Split('/')[0];
            }

            url = (isHttps) ? $"https://{url}" : $"http://{url}";

            return url;
        }
    }
}
