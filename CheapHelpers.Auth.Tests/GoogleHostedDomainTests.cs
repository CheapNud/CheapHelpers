using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.Auth.Tests;

public class GoogleHostedDomainTests
{
    private const string HostedDomain = "example.com";
    private const string ExternalSignInPath = "/test/external-signin";
    private const string CallbackPath = "/auth/google-callback";
    private const string LoginPath = "/Account/Login";

    // Matches the private external cookie scheme name in OAuthRegistrationExtensions.
    private const string ExternalCookieScheme = "ExternalCookie";

    [Fact]
    public async Task Rejects_a_sign_in_without_an_email()
    {
        var location = await RunCallbackAsync(email: null, hostedDomain: HostedDomain);

        AssertRejected(location);
    }

    [Fact]
    public async Task Rejects_a_domain_email_without_the_hd_claim()
    {
        var location = await RunCallbackAsync(email: "user@example.com", hostedDomain: null);

        AssertRejected(location);
    }

    [Fact]
    public async Task Rejects_a_domain_email_with_another_hd_claim()
    {
        var location = await RunCallbackAsync(email: "user@example.com", hostedDomain: "other.com");

        AssertRejected(location);
    }

    [Fact]
    public async Task Rejects_an_email_at_another_domain_with_a_matching_hd_claim()
    {
        var location = await RunCallbackAsync(email: "user@other.com", hostedDomain: HostedDomain);

        AssertRejected(location);
    }

    [Fact]
    public async Task Accepts_a_domain_email_with_a_matching_hd_claim()
    {
        var location = await RunCallbackAsync(email: "user@example.com", hostedDomain: HostedDomain);

        Assert.Equal("/", location);
    }

    private static void AssertRejected(string? location)
    {
        Assert.NotNull(location);
        Assert.StartsWith(LoginPath, location);
        Assert.Contains(HostedDomain, Uri.UnescapeDataString(location));
    }

    /// <summary>
    /// Signs a principal into the external cookie, then calls the Google callback with that cookie and returns the redirect target.
    /// </summary>
    private static async Task<string?> RunCallbackAsync(string? email, string? hostedDomain)
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var app = await TestApp.StartAsync(
            builder =>
            {
                builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
                builder.Services.AddGoogleAuth(google =>
                {
                    google.ClientId = "id";
                    google.ClientSecret = "secret";
                    google.HostedDomain = HostedDomain;
                });
            },
            webApp =>
            {
                webApp.MapOAuthEndpoints();
                webApp.MapGet(ExternalSignInPath, SignInExternalAsync);
            },
            cancellationToken);

        var client = app.GetTestClient();

        var query = QueryString.Empty;
        if (email is not null)
            query = query.Add("email", email);
        if (hostedDomain is not null)
            query = query.Add("hd", hostedDomain);

        var signInResponse = await client.GetAsync(ExternalSignInPath + query, cancellationToken);
        signInResponse.EnsureSuccessStatusCode();
        var cookies = signInResponse.Headers.GetValues("Set-Cookie").Select(header => header.Split(';')[0]);

        using var callbackRequest = new HttpRequestMessage(HttpMethod.Get, CallbackPath);
        callbackRequest.Headers.Add("Cookie", string.Join("; ", cookies));
        var callbackResponse = await client.SendAsync(callbackRequest, cancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, callbackResponse.StatusCode);
        return callbackResponse.Headers.Location?.OriginalString;
    }

    private static async Task<IResult> SignInExternalAsync(HttpContext httpContext, string? email, string? hd)
    {
        List<Claim> claims = [new(ClaimTypes.NameIdentifier, "123")];
        if (email is not null)
            claims.Add(new Claim(ClaimTypes.Email, email));
        if (hd is not null)
            claims.Add(new Claim("hd", hd));

        await httpContext.SignInAsync(ExternalCookieScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, ExternalCookieScheme)));
        return Results.Ok();
    }
}
