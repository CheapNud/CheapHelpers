using System.Net;
using CheapHelpers.Blazor.Extensions;
using CheapHelpers.Blazor.Pages.Account;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.Blazor.Tests;

public class ChangePasswordHookTests
{
    private const string SignInPath = "/test/signin";
    private const string ChangePasswordPath = "/Account/ChangePassword";
    private const string UserName = "user@example.com";
    private const string OldPassword = "Old-Passw0rd!";
    private const string NewPassword = "New-Passw0rd!";
    private const string HookTarget = "/after-change";

    [Fact]
    public async Task Calls_the_hook_once_with_the_user_and_redirects_to_its_path()
    {
        List<string> calledUserIds = [];

        var (location, userId) = await ChangePasswordAsync(OldPassword, user =>
        {
            calledUserIds.Add(user.Id);
            return HookTarget;
        });

        Assert.Equal(HookTarget, location);
        Assert.Equal(userId, Assert.Single(calledUserIds));
    }

    [Fact]
    public async Task Redirects_home_when_the_hook_returns_null()
    {
        var (location, _) = await ChangePasswordAsync(OldPassword, _ => null);

        Assert.Equal("/", location);
    }

    [Fact]
    public async Task Skips_the_hook_when_the_old_password_is_wrong()
    {
        var called = false;

        var (location, _) = await ChangePasswordAsync("Wrong-Passw0rd!", _ =>
        {
            called = true;
            return HookTarget;
        });

        Assert.False(called);
        Assert.NotNull(location);
        Assert.StartsWith(ChangePasswordPath + "?failed=", location);
    }

    /// <summary>
    /// Signs in a fresh user, posts the change-password form with the given old password and returns the redirect target.
    /// </summary>
    private static async Task<(string? Location, string UserId)> ChangePasswordAsync(string oldPassword, Func<TestUser, string?> hook)
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var app = await TestApp.StartAsync(
            builder =>
            {
                builder.Services.AddTestIdentity();
                builder.Services.AddAuthorization();
                builder.Services.AddAntiforgery();
                builder.Services.AddSingleton(new AccountRouteOptions());
            },
            webApp =>
            {
                webApp.UseAuthentication();
                webApp.UseAuthorization();
                webApp.UseAntiforgery();
                webApp.MapCheapAccountEndpoints<TestUser>(options => options.AfterPasswordChanged = async (user, _) =>
                {
                    await Task.Yield();
                    return hook(user);
                });
                webApp.MapGet(SignInPath, SignInAsync);
            },
            cancellationToken);

        var client = app.GetTestClient();

        var signInResponse = await client.GetAsync(SignInPath, cancellationToken);
        signInResponse.EnsureSuccessStatusCode();
        var requestToken = await signInResponse.Content.ReadAsStringAsync(cancellationToken);
        var cookies = signInResponse.Headers.GetValues("Set-Cookie").Select(header => header.Split(';')[0]);

        using var changeRequest = new HttpRequestMessage(HttpMethod.Post, ChangePasswordPath);
        changeRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = requestToken,
            ["OldPassword"] = oldPassword,
            ["NewPassword"] = NewPassword,
            ["ConfirmPassword"] = NewPassword
        });
        changeRequest.Headers.Add("Cookie", string.Join("; ", cookies));
        var changeResponse = await client.SendAsync(changeRequest, cancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, changeResponse.StatusCode);

        using var scope = app.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TestUser>>();
        var user = await userManager.FindByNameAsync(UserName);
        Assert.NotNull(user);
        return (changeResponse.Headers.Location?.OriginalString, user.Id);
    }

    /// <summary>
    /// Creates the user, signs it in and returns an antiforgery request token bound to that user.
    /// </summary>
    private static async Task<IResult> SignInAsync(HttpContext httpContext, UserManager<TestUser> userManager,
        SignInManager<TestUser> signInManager, IAntiforgery antiforgery)
    {
        var user = new TestUser { UserName = UserName, Email = UserName };
        var created = await userManager.CreateAsync(user, OldPassword);
        if (!created.Succeeded)
            return Results.BadRequest();

        await signInManager.SignInAsync(user, isPersistent: false);

        // The token is bound to the current principal, which SignInAsync does not replace on this request
        httpContext.User = await signInManager.CreateUserPrincipalAsync(user);
        return Results.Text(antiforgery.GetAndStoreTokens(httpContext).RequestToken);
    }
}
