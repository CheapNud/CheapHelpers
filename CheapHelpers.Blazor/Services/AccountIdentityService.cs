using CheapHelpers.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.Blazor.Services;

/// <summary>
/// Authenticator key info for 2FA setup
/// </summary>
public sealed record AuthenticatorKeyInfo(string SharedKey, string? Email);

/// <summary>
/// Non-generic identity operations for the shared Account pages. Pages can't be generic over
/// TUser (the router needs concrete types), so they inject this interface and the DI
/// registration supplies the user-typed implementation.
/// </summary>
public interface IAccountIdentityService
{
    /// <summary>
    /// Gets the user's authenticator key for 2FA setup, generating one when missing.
    /// Returns null when the user doesn't exist.
    /// </summary>
    Task<AuthenticatorKeyInfo?> GetOrCreateAuthenticatorKeyAsync(string userId);
}

/// <summary>
/// Scope-per-call implementation: resolves UserManager from a fresh DI scope per operation,
/// same pattern as UserRepo's identity helpers — safe from Blazor Server circuits.
/// </summary>
public class AccountIdentityService<TUser>(IServiceScopeFactory scopeFactory) : IAccountIdentityService
    where TUser : CheapUser
{
    public async Task<AuthenticatorKeyInfo?> GetOrCreateAuthenticatorKeyAsync(string userId)
    {
        using var scope = scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return null;
        }

        var sharedKey = await userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(sharedKey))
        {
            await userManager.ResetAuthenticatorKeyAsync(user);
            sharedKey = await userManager.GetAuthenticatorKeyAsync(user);
        }

        if (string.IsNullOrEmpty(sharedKey))
        {
            return null;
        }

        return new AuthenticatorKeyInfo(sharedKey, await userManager.GetEmailAsync(user));
    }
}
