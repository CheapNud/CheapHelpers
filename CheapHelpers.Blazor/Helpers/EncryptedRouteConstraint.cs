using CheapHelpers.Helpers.Encryption;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CheapHelpers.Blazor.Helpers
{
    public class EncryptedRouteConstraint : IRouteConstraint
    {
        public bool Match(
            HttpContext httpContext,
            IRouter route,
            string routeKey,
            RouteValueDictionary values,
            RouteDirection routeDirection)
        {
            if (!values.TryGetValue(routeKey, out var value) || value == null)
            {
                return false;
            }

            string encryptedValue = value.ToString();

            try
            {
                // Decrypt the parameter value when route is matched
                // Using deterministic Decrypt() is intentional here - route parameters need deterministic encryption
                // to match URLs correctly. This is a valid use case for the static IV approach.
#pragma warning disable CS0618 // Type or member is obsolete
                var decryptedValue = EncryptionHelper.Decrypt(encryptedValue);
#pragma warning restore CS0618 // Type or member is obsolete
                values[routeKey] = decryptedValue;
                return true;
            }
            catch
            {
                // If decryption fails, return false
                return false;
            }
        }
    }

}
