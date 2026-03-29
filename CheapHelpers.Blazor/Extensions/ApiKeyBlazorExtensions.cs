using CheapHelpers.Blazor.Middleware;
using Microsoft.AspNetCore.Builder;

namespace CheapHelpers.Blazor.Extensions;

/// <summary>
/// Extension methods for registering API key middleware in the ASP.NET Core pipeline.
/// </summary>
public static class ApiKeyBlazorExtensions
{
    /// <summary>
    /// Adds the API key validation and rate-limiting middleware to the request pipeline.
    /// Should be placed after <c>UseAuthentication()</c> and before <c>UseAuthorization()</c>.
    /// Requires <c>AddCheapApiKeys&lt;TUser&gt;()</c> to be called in service registration.
    /// </summary>
    public static IApplicationBuilder UseCheapApiKeyMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ApiKeyMiddleware>();
    }
}
