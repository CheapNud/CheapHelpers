using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.Auth.Testing;

/// <summary>
/// Registers <see cref="TestAuthenticationHandler"/> and sets its header on a client.
/// </summary>
public static class TestAuthenticationExtensions
{
    /// <summary>Registers the Test scheme as the default authenticate and challenge scheme.</summary>
    public static AuthenticationBuilder AddTestAuthentication(this IServiceCollection services) =>
        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.SchemeName, null);

    /// <summary>Makes every later request from this client sign in as <paramref name="userId"/> with the extra claims.</summary>
    /// <returns>The same client, for chaining.</returns>
    public static HttpClient AsUser(this HttpClient client, string userId, params ReadOnlySpan<(string Type, string Value)> claims)
    {
        var parameter = new StringBuilder(userId);
        foreach (var (type, claimValue) in claims)
        {
            parameter.Append(';').Append(type).Append('=').Append(claimValue);
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthenticationHandler.SchemeName, parameter.ToString());
        return client;
    }
}
