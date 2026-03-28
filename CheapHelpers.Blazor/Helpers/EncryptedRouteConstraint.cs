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
                var decryptedValue = EncryptionHelper.DecryptDeterministic(encryptedValue);
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
