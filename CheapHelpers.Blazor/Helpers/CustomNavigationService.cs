using CheapHelpers.Helpers.Encryption;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace CheapHelpers.Blazor.Helpers
{
    public class CustomNavigationService(NavigationManager navigationManager, AuthenticationStateProvider authenticationStateProvider)
    {
        // Dictionary mapping roles to the specific parameters that need to be encrypted for that role
        private readonly Dictionary<string, List<string>> _roleBasedEncryptionParams = new()
        {
            { "Roles.ServiceExternal", new List<string> { "OrderId" } },
            //{ "ServiceSupplier", new List<string> { "serviceId", "orderId" } }
        };

        private async Task<List<string>> GetParametersToEncryptAsync()
        {
            var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            // If the user is not authenticated, return an empty list (no encryption)
            if (!user.Identity.IsAuthenticated)
                return new List<string>();

            // Get the list of parameters to encrypt based on the user's roles
            foreach (var role in _roleBasedEncryptionParams.Keys)
            {
                if (user.IsInRole(role))
                {
                    return _roleBasedEncryptionParams[role];
                }
            }

            // If the user does not have any of the roles that require encryption, return an empty list
            return [];
        }

        public async Task NavigateToUrlWithSelectiveEncryptionAsync(string url, Dictionary<string, object> parameters)
        {
            var parametersToEncrypt = await GetParametersToEncryptAsync();

            var finalParams = new Dictionary<string, string>();

            foreach (var param in parameters)
            {
                // Encrypt only if the parameter should be encrypted for the user's role
                if (parametersToEncrypt.Contains(param.Key))
                {
                    // Encrypt the parameter
                    finalParams.Add(param.Key, EncryptionHelper.Encrypt(param.Value.ToString()));
                }
                else
                {
                    // Leave other parameters as-is
                    finalParams.Add(param.Key, param.Value.ToString());
                }
            }

            // Construct the query string
            var queryString = string.Join("&", finalParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));

            // Append query string to the URL
            var fullUrl = $"{url}?{queryString}";

            // Navigate to the final URL
            navigationManager.NavigateTo(fullUrl);
        }

        public async Task<Dictionary<string, string>> GetDecryptedParametersFromUrlAsync()
        {
            var parametersToDecrypt = await GetParametersToEncryptAsync(); // Same list used for decryption

            var uri = new Uri(navigationManager.Uri);
            var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var decryptedParams = new Dictionary<string, string>();

            foreach (var key in queryParams.AllKeys)
            {
                if (parametersToDecrypt.Contains(key))
                {
                    try
                    {
                        // Decrypt the parameter
                        decryptedParams.Add(key, EncryptionHelper.Decrypt(queryParams[key]));
                    }
                    catch (Exception ex)
                    {
                        // Handle decryption failure (log error, show error message, etc.)
                        Console.WriteLine($"Decryption failed for parameter '{key}': {ex.Message}");
                        decryptedParams.Add(key, "DecryptionFailed");
                    }
                }
                else
                {
                    // For non-encrypted parameters, add them directly
                    decryptedParams.Add(key, queryParams[key]);
                }
            }

            return decryptedParams;
        }
    }

}
