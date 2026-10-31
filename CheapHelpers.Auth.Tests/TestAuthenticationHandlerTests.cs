using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CheapHelpers.Auth.Tests;

public class TestAuthenticationHandlerTests
{
    private const string ClaimsPath = "/claims";

    [Fact]
    public async Task Signs_in_as_the_header_user_with_the_extra_claims()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await StartAsync(Environments.Development, cancellationToken);
        using var client = app.GetTestClient().AsUser("u1", ("role", "Admin"), ("email", "a@b.c"));

        var claims = (await client.GetStringAsync(ClaimsPath, cancellationToken)).Split('\n');

        Assert.Equal([$"{ClaimTypes.NameIdentifier}=u1", "sub=u1", "role=Admin", "email=a@b.c"], claims);
    }

    [Fact]
    public async Task Answers_401_without_the_header()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await StartAsync(Environments.Development, cancellationToken);
        using var client = app.GetTestClient();

        using var response = await client.GetAsync(ClaimsPath, cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Answers_401_in_Production_even_with_a_valid_header()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await StartAsync(Environments.Production, cancellationToken);
        using var client = app.GetTestClient().AsUser("u1");

        using var response = await client.GetAsync(ClaimsPath, cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static Task<WebApplication> StartAsync(string environmentName, CancellationToken cancellationToken) =>
        TestWebApp.StartAsync(
            builder =>
            {
                builder.Services.AddTestAuthentication();
                builder.Services.AddAuthorization();
            },
            app =>
            {
                app.UseAuthentication();
                app.UseAuthorization();
                app.MapGet(ClaimsPath, (HttpContext httpContext) =>
                    string.Join('\n', httpContext.User.Claims.Select(claim => $"{claim.Type}={claim.Value}"))).RequireAuthorization();
            },
            cancellationToken,
            environmentName);
}
