using System.Net;
using CheapHelpers.Auth.Plex;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.Auth.Tests;

public class PlexAuthEndpointTests
{
    [Fact]
    public async Task A_blank_callback_base_url_builds_the_callback_from_the_request_host()
    {
        var forwardUrl = await StartSignInAsync(callbackBaseUrl: "");

        Assert.Equal("http://localhost/auth/plex-callback", forwardUrl);
    }

    [Fact]
    public async Task A_configured_callback_base_url_is_used_without_its_trailing_slash()
    {
        var forwardUrl = await StartSignInAsync(callbackBaseUrl: "https://public.example/");

        Assert.Equal("https://public.example/auth/plex-callback", forwardUrl);
    }

    private static async Task<string?> StartSignInAsync(string callbackBaseUrl)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var plexOptions = new PlexAuthOptions { CallbackBaseUrl = callbackBaseUrl };

        await using var app = await TestApp.StartAsync(
            builder =>
            {
                builder.Services.AddSingleton(plexOptions);
                builder.Services.AddSingleton<IPlexAuthService, FakePlexAuthService>();
            },
            webApp => webApp.MapPlexAuthEndpoints(),
            cancellationToken);

        var response = await app.GetTestClient().GetAsync(plexOptions.StartPath, cancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return response.Headers.Location?.OriginalString;
    }

    /// <summary>
    /// Returns the forward URL as the redirect target, so the test can read the callback the endpoint built.
    /// </summary>
    private sealed class FakePlexAuthService : IPlexAuthService
    {
        public string ProviderName => "Plex";

        public async Task<(long PinId, string PinCode)> CreatePinAsync(CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return (1, "code");
        }

        public string GetAuthRedirectUrl(string pinCode, string forwardUrl) => forwardUrl;

        public Task<PlexPin?> CheckPinAsync(long pinId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PlexUser?> GetUserAsync(string authToken, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> HasServerAccessAsync(long plexUserId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
