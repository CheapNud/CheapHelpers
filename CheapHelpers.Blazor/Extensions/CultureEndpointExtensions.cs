using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Routing;

namespace CheapHelpers.Blazor.Extensions;

/// <summary>
/// Minimal API endpoint behind <c>CultureSelector</c>: stores the chosen culture in the
/// request-culture cookie and sends the browser back to the page it came from.
/// </summary>
public static class CultureEndpointExtensions
{
    /// <summary>
    /// Maps <c>GET /Culture/Set?culture=&amp;redirectUri=</c>. Only local redirect paths are followed;
    /// anything else redirects to <c>/</c>.
    /// </summary>
    public static IEndpointRouteBuilder MapCheapCultureEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/Culture/Set", (string? culture, string? redirectUri, HttpContext httpContext) =>
        {
            if (culture is not null)
            {
                httpContext.Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture, culture)));
            }

            return Results.Redirect(IsLocalPath(redirectUri) ? redirectUri! : "/");
        });

        return endpoints;
    }

    // Same rule LocalRedirect enforced: a single leading slash, not "//" or "/\" (protocol-relative),
    // and no control characters (browsers strip tabs, so "/<tab>/evil" would become "//evil")
    private static bool IsLocalPath(string? path) =>
        path is ['/'] or ['/', not ('/' or '\\'), ..] && !path.Any(char.IsControl);
}
