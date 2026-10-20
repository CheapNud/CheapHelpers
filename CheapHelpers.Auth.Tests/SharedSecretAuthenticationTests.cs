using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.Auth.Tests;

public class SharedSecretAuthenticationTests
{
    private const string Secret = "correct-horse-battery";
    private const string ProtectedPath = "/protected";

    [Fact]
    public async Task Accepts_the_secret_in_the_header()
    {
        var response = await SendAsync(request => request.Headers.Add(SharedSecretDefaults.HeaderName, Secret));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(SharedSecretDefaults.PrincipalName, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Accepts_the_secret_as_a_bearer_token()
    {
        var response = await SendAsync(request => request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Secret));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Rejects_a_bearer_token_when_bearer_is_off()
    {
        var response = await SendAsync(
            request => request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Secret),
            allowBearer: false);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Rejects_a_wrong_secret_of_the_same_length()
    {
        var wrongSecret = new string('x', Secret.Length);

        var response = await SendAsync(request => request.Headers.Add(SharedSecretDefaults.HeaderName, wrongSecret));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Rejects_a_secret_of_a_different_length()
    {
        var response = await SendAsync(request => request.Headers.Add(SharedSecretDefaults.HeaderName, Secret + "-longer"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Rejects_a_request_without_the_header()
    {
        var response = await SendAsync(_ => { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<HttpResponseMessage> SendAsync(Action<HttpRequestMessage> prepareRequest, bool allowBearer = true)
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var app = await TestApp.StartAsync(
            builder =>
            {
                builder.Services.AddAuthentication(SharedSecretDefaults.AuthenticationScheme)
                    .AddSharedSecret(sharedSecretOptions =>
                    {
                        sharedSecretOptions.Secret = Secret;
                        sharedSecretOptions.AllowBearer = allowBearer;
                    });
                builder.Services.AddAuthorization();
            },
            webApp =>
            {
                webApp.UseAuthentication();
                webApp.UseAuthorization();
                webApp.MapGet(ProtectedPath, (HttpContext httpContext) => httpContext.User.Identity!.Name).RequireAuthorization();
            },
            cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, ProtectedPath);
        prepareRequest(request);
        var response = await app.GetTestClient().SendAsync(request, cancellationToken);
        await response.Content.LoadIntoBufferAsync(cancellationToken);
        return response;
    }
}
