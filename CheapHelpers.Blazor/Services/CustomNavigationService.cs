using CheapHelpers.Blazor.Helpers;
using CheapHelpers.Helpers.Encryption;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Diagnostics;
using System.Web;

namespace CheapHelpers.Blazor.Services
{

    /// <summary>
    /// Provides navigation services with configurable role-based selective parameter encryption
    /// </summary>
    public class CustomNavigationService
    {
        private readonly NavigationManager _navigationManager;
        private readonly AuthenticationStateProvider _authenticationStateProvider;
        private readonly NavigationEncryptionConfiguration _configuration;

        // Cache for performance - stores the last computed encryption parameters
        private (DateTime CacheTime, List<string> Parameters)? _encryptionParamsCache;

        public CustomNavigationService(
            NavigationManager navigationManager,
            AuthenticationStateProvider authenticationStateProvider,
            NavigationEncryptionConfiguration configuration)
        {
            _navigationManager = navigationManager;
            _authenticationStateProvider = authenticationStateProvider;
            _configuration = configuration;
        }

        /// <summary>
        /// Gets the list of parameters that should be encrypted based on the current user's roles
        /// </summary>
        /// <param name="forceRefresh">If true, bypasses cache and retrieves fresh data</param>
        /// <returns>List of parameter names to encrypt</returns>
        private async Task<List<string>> GetParametersToEncryptAsync(bool forceRefresh = false)
        {
            // Check cache first (unless forced refresh)
            if (!forceRefresh && _encryptionParamsCache.HasValue &&
                DateTime.Now.Subtract(_encryptionParamsCache.Value.CacheTime).TotalMinutes < _configuration.CacheDurationMinutes)
            {
                return _encryptionParamsCache.Value.Parameters;
            }

            try
            {
                var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;

                // If the user is not authenticated, return an empty list (no encryption)
                if (!user.Identity?.IsAuthenticated == true)
                {
                    var emptyList = new List<string>();
                    _encryptionParamsCache = (DateTime.Now, emptyList);
                    return emptyList;
                }

                // Get the list of parameters to encrypt based on the user's roles
                foreach (var (role, parameters) in _configuration.RoleBasedEncryptionParams)
                {
                    if (user.IsInRole(role))
                    {
                        _encryptionParamsCache = (DateTime.Now, parameters);
                        return parameters;
                    }
                }

                // If the user does not have any of the roles that require encryption, return an empty list
                var defaultList = new List<string>();
                _encryptionParamsCache = (DateTime.Now, defaultList);
                return defaultList;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting parameters to encrypt: {ex.Message}");
                return [];
            }
        }

        /// <summary>
        /// Navigates to a URL with selective parameter encryption based on user roles
        /// </summary>
        /// <param name="url">Base URL to navigate to</param>
        /// <param name="parameters">Parameters to include in the URL</param>
        /// <param name="forceRefresh">Force refresh of role-based encryption parameters</param>
        public async Task NavigateToUrlWithSelectiveEncryptionAsync(
            string url,
            Dictionary<string, object> parameters,
            bool forceRefresh = false)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(url);
            ArgumentNullException.ThrowIfNull(parameters);

            try
            {
                var parametersToEncrypt = await GetParametersToEncryptAsync(forceRefresh);
                var finalParams = new Dictionary<string, string>();

                foreach (var (key, value) in parameters)
                {
                    if (string.IsNullOrEmpty(key) || value == null)
                    {
                        Debug.WriteLine($"Skipping null or empty parameter: {key}");
                        continue;
                    }

                    // Encrypt only if the parameter should be encrypted for the user's role
                    if (parametersToEncrypt.Contains(key))
                    {
                        try
                        {
                            // Encrypt the parameter
                            var encryptedValue = EncryptionHelper.Encrypt(value.ToString()!);
                            finalParams.Add(key, encryptedValue);
                            Debug.WriteLine($"Encrypted parameter: {key}");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Encryption failed for parameter '{key}': {ex.Message}");
                            // Fall back to unencrypted value if encryption fails
                            finalParams.Add(key, value.ToString()!);
                        }
                    }
                    else
                    {
                        // Leave other parameters as-is
                        finalParams.Add(key, value.ToString()!);
                    }
                }

                var fullUrl = BuildUrlWithParameters(url, finalParams);

                Debug.WriteLine($"Navigating to: {fullUrl}");
                _navigationManager.NavigateTo(fullUrl);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Navigation failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Overload for simple string parameters
        /// </summary>
        public async Task NavigateToUrlWithSelectiveEncryptionAsync(
            string url,
            Dictionary<string, string> parameters,
            bool forceRefresh = false)
        {
            var objectParams = parameters.ToDictionary(
                kvp => kvp.Key,
                kvp => (object)kvp.Value
            );
            await NavigateToUrlWithSelectiveEncryptionAsync(url, objectParams, forceRefresh);
        }

        /// <summary>
        /// Retrieves and decrypts parameters from the current URL based on user roles
        /// </summary>
        /// <param name="forceRefresh">Force refresh of role-based encryption parameters</param>
        /// <returns>Dictionary of decrypted parameters</returns>
        public async Task<Dictionary<string, string>> GetDecryptedParametersFromUrlAsync(bool forceRefresh = false)
        {
            try
            {
                var parametersToDecrypt = await GetParametersToEncryptAsync(forceRefresh); // Same list used for decryption

                if (!Uri.TryCreate(_navigationManager.Uri, UriKind.Absolute, out var uri))
                {
                    Debug.WriteLine($"Invalid URI: {_navigationManager.Uri}");
                    return [];
                }

                var queryParams = HttpUtility.ParseQueryString(uri.Query);
                var decryptedParams = new Dictionary<string, string>();

                foreach (string key in queryParams.AllKeys)
                {
                    if (string.IsNullOrEmpty(key))
                        continue;

                    var value = queryParams[key];
                    if (string.IsNullOrEmpty(value))
                    {
                        decryptedParams[key] = string.Empty;
                        continue;
                    }

                    if (parametersToDecrypt.Contains(key))
                    {
                        try
                        {
                            // Decrypt the parameter
                            var decryptedValue = EncryptionHelper.Decrypt(value);
                            decryptedParams[key] = decryptedValue;
                            Debug.WriteLine($"Decrypted parameter: {key}");
                        }
                        catch (Exception ex)
                        {
                            // Handle decryption failure (log error, show error message, etc.)
                            Debug.WriteLine($"Decryption failed for parameter '{key}': {ex.Message}");
                            decryptedParams[key] = "DecryptionFailed";
                        }
                    }
                    else
                    {
                        // For non-encrypted parameters, add them directly
                        decryptedParams[key] = value;
                    }
                }

                return decryptedParams;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting decrypted parameters: {ex.Message}");
                return [];
            }
        }

        /// <summary>
        /// Gets a specific decrypted parameter from the current URL
        /// </summary>
        /// <param name="parameterName">Name of the parameter to retrieve</param>
        /// <param name="defaultValue">Default value if parameter is not found</param>
        /// <returns>Decrypted parameter value or default value</returns>
        public async Task<string> GetDecryptedParameterAsync(string parameterName, string defaultValue = "")
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(parameterName);

            var parameters = await GetDecryptedParametersFromUrlAsync();
            return parameters.TryGetValue(parameterName, out var value) ? value : defaultValue;
        }

        /// <summary>
        /// Gets a specific decrypted parameter and attempts to convert it to the specified type
        /// </summary>
        /// <typeparam name="T">Type to convert the parameter to</typeparam>
        /// <param name="parameterName">Name of the parameter to retrieve</param>
        /// <param name="defaultValue">Default value if parameter is not found or conversion fails</param>
        /// <returns>Converted parameter value or default value</returns>
        public async Task<T> GetDecryptedParameterAsync<T>(string parameterName, T defaultValue = default!)
        {
            try
            {
                var stringValue = await GetDecryptedParameterAsync(parameterName);

                if (string.IsNullOrEmpty(stringValue) || stringValue == "DecryptionFailed")
                    return defaultValue;

                return (T)Convert.ChangeType(stringValue, typeof(T));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error converting parameter '{parameterName}' to type {typeof(T).Name}: {ex.Message}");
                return defaultValue;
            }
        }

        /// <summary>
        /// Builds a complete URL with query parameters
        /// </summary>
        /// <param name="baseUrl">Base URL</param>
        /// <param name="parameters">Parameters to append</param>
        /// <returns>Complete URL with parameters</returns>
        private static string BuildUrlWithParameters(string baseUrl, Dictionary<string, string> parameters)
        {
            if (!parameters.Any())
                return baseUrl;

            var queryString = string.Join("&",
                parameters.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}")
            );

            var separator = baseUrl.Contains('?') ? "&" : "?";
            return $"{baseUrl}{separator}{queryString}";
        }

        /// <summary>
        /// Clears the encryption parameters cache
        /// </summary>
        public void ClearCache()
        {
            _encryptionParamsCache = null;
            Debug.WriteLine("Encryption parameters cache cleared");
        }

        /// <summary>
        /// Checks if the current user has any roles that require parameter encryption
        /// </summary>
        /// <returns>True if the user has encryption-enabled roles</returns>
        public async Task<bool> HasEncryptionEnabledRolesAsync()
        {
            var parametersToEncrypt = await GetParametersToEncryptAsync();
            return parametersToEncrypt.Count > 0;
        }

        /// <summary>
        /// Gets the current role-based encryption configuration (read-only)
        /// </summary>
        /// <returns>Read-only dictionary of role configurations</returns>
        public IReadOnlyDictionary<string, List<string>> GetRoleBasedEncryptionConfiguration()
        {
            return _configuration.RoleBasedEncryptionParams.AsReadOnly();
        }
    }
}