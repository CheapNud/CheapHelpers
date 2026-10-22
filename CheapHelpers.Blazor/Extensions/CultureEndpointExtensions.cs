using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CheapHelpers.Blazor.Extensions;

/// <summary>
/// Minimal API endpoint behind <c>CultureSelector</c>: stores the chosen culture in the
/// request-culture cookie and sends the browser back to the page it came from.
/// </summary>
public static class CultureEndpointExtensions
{
    private const int CookieLifetimeYears = 1;

    /// <summary>
    /// Maps <c>GET /Culture/Set?culture=&amp;redirectUri=</c> as an anonymous endpoint. When
    /// <see cref="RequestLocalizationOptions"/> is configured in DI, only its supported UI cultures (or its
    /// supported cultures when the UI list is empty) are stored; otherwise any culture .NET knows is
    /// stored. Unknown cultures are ignored. The cookie is essential and lasts a year. Only local redirect paths are followed; anything else redirects to <c>/</c>.
    /// </summary>
    public static IEndpointRouteBuilder MapCheapCultureEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/Culture/Set", (string? culture, string? redirectUri, HttpContext httpContext) =>
        {
            var resolved = ResolveCulture(culture, httpContext.RequestServices);
            if (resolved is not null)
            {
                httpContext.Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(resolved, resolved)),
                    new CookieOptions
                    {
                        IsEssential = true,
                        Expires = DateTimeOffset.UtcNow.AddYears(CookieLifetimeYears),
                        SameSite = SameSiteMode.Lax,
                    });
            }

            return Results.Redirect(IsLocalPath(redirectUri) ? redirectUri! : "/");
        }).AllowAnonymous();

        return endpoints;
    }

    private static string? ResolveCulture(string? culture, IServiceProvider services)
    {
        if (string.IsNullOrWhiteSpace(culture))
            return null;

        // IOptions<T> always resolves and its defaults hold only the server culture, so the supported
        // list is trusted only when the app configured it (an app that passes options straight to
        // UseRequestLocalization registers nothing here and falls through to the lookup below)
        if (services.GetServices<IConfigureOptions<RequestLocalizationOptions>>().Any())
        {
            var options = services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
            // An app that cleared the UI list but kept SupportedCultures still means that list
            var supported = options.SupportedUICultures is { Count: > 0 } ? options.SupportedUICultures : options.SupportedCultures;
            if (supported is { Count: > 0 })
                return supported.FirstOrDefault(c => string.Equals(c.Name, culture, StringComparison.OrdinalIgnoreCase))?.Name;
        }

        try
        {
            // predefinedOnly: ICU otherwise accepts any well-formed name such as "zz"
            return CultureInfo.GetCultureInfo(culture, predefinedOnly: true).Name;
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }

    // Same rule LocalRedirect enforced: a single leading slash, not "//" or "/\" (protocol-relative),
    // and no control characters (browsers strip tabs, so "/<tab>/evil" would become "//evil")
    private static bool IsLocalPath(string? path) =>
        path is ['/'] or ['/', not ('/' or '\\'), ..] && !path.Any(char.IsControl);
}
