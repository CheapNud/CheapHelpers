using CheapHelpers.Models.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.Blazor.Services;

/// <summary>
/// Authenticator key info for 2FA setup
/// </summary>
public sealed record AuthenticatorKeyInfo(string SharedKey, string? Email);

/// <summary>
/// Result of enabling the authenticator. RecoveryCodes is set only when the user had none yet.
/// </summary>
public sealed record EnableAuthenticatorResult(bool Succeeded, string[]? RecoveryCodes);

/// <summary>
/// The user's linked external logins and the external providers not linked yet.
/// </summary>
public sealed record ExternalLoginsInfo(IList<UserLoginInfo> CurrentLogins, IList<AuthenticationScheme> AvailableLogins);

/// <summary>
/// Two-factor state for the account pages.
/// </summary>
public sealed record TwoFactorStatus(bool IsTwoFactorEnabled, int RecoveryCodesCount, bool HasAuthenticatorKey);

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

    /// <summary>
    /// Verifies the authenticator code and enables two-factor. Generates recovery codes when the
    /// user has none. A missing user or an empty code returns <c>Succeeded = false</c>.
    /// </summary>
    Task<EnableAuthenticatorResult> EnableAuthenticatorAsync(string userId, string? code);

    /// <summary>
    /// Gets the user's external logins and the providers not linked yet. Null when the user doesn't exist.
    /// </summary>
    Task<ExternalLoginsInfo?> GetExternalLoginsAsync(string userId);

    /// <summary>
    /// Gets the user's [PersonalData] properties and external login keys. Null when the user doesn't exist.
    /// </summary>
    Task<Dictionary<string, string>?> GetPersonalDataAsync(string userId);

    /// <summary>
    /// Gets the user's two-factor state. Null when the user doesn't exist.
    /// </summary>
    Task<TwoFactorStatus?> GetTwoFactorStatusAsync(string userId);

    /// <summary>
    /// Replaces the user's recovery codes with new ones. Null when the user doesn't exist.
    /// </summary>
    Task<string[]?> GenerateRecoveryCodesAsync(string userId);
}

/// <summary>
/// Scope-per-call implementation: resolves UserManager from a fresh DI scope per operation,
/// same pattern as UserRepo's identity helpers — safe from Blazor Server circuits.
/// </summary>
public class AccountIdentityService<TUser>(IServiceScopeFactory scopeFactory) : IAccountIdentityService
    where TUser : CheapUser
{
    private const int RecoveryCodesCount = 10;

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

    public async Task<EnableAuthenticatorResult> EnableAuthenticatorAsync(string userId, string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return new EnableAuthenticatorResult(false, null);
        }

        using var scope = scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return new EnableAuthenticatorResult(false, null);
        }

        var verificationCode = code.Replace(" ", string.Empty).Replace("-", string.Empty);
        var isTokenValid = await userManager.VerifyTwoFactorTokenAsync(
            user,
            userManager.Options.Tokens.AuthenticatorTokenProvider,
            verificationCode);

        if (!isTokenValid)
        {
            return new EnableAuthenticatorResult(false, null);
        }

        await userManager.SetTwoFactorEnabledAsync(user, true);

        // Generate recovery codes if none exist
        if (await userManager.CountRecoveryCodesAsync(user) == 0)
        {
            var recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, RecoveryCodesCount);
            return new EnableAuthenticatorResult(true, recoveryCodes?.ToArray());
        }

        return new EnableAuthenticatorResult(true, null);
    }

    public async Task<ExternalLoginsInfo?> GetExternalLoginsAsync(string userId)
    {
        using var scope = scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<TUser>>();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return null;
        }

        var currentLogins = await userManager.GetLoginsAsync(user);
        var availableSchemes = await signInManager.GetExternalAuthenticationSchemesAsync();
        var otherLogins = availableSchemes
            .Where(auth => currentLogins.All(ul => auth.Name != ul.LoginProvider))
            .ToList();

        return new ExternalLoginsInfo(currentLogins, otherLogins);
    }

    public async Task<Dictionary<string, string>?> GetPersonalDataAsync(string userId)
    {
        using var scope = scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return null;
        }

        // Properties marked with PersonalDataAttribute
        var personalData = typeof(TUser)
            .GetProperties()
            .Where(prop => Attribute.IsDefined(prop, typeof(PersonalDataAttribute)))
            .ToDictionary(prop => prop.Name, prop => prop.GetValue(user)?.ToString() ?? "null");

        // External login information
        var logins = await userManager.GetLoginsAsync(user);
        foreach (var login in logins)
        {
            personalData.Add($"{login.LoginProvider} external login provider key", login.ProviderKey);
        }

        return personalData;
    }

    public async Task<TwoFactorStatus?> GetTwoFactorStatusAsync(string userId)
    {
        using var scope = scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return null;
        }

        return new TwoFactorStatus(
            await userManager.GetTwoFactorEnabledAsync(user),
            await userManager.CountRecoveryCodesAsync(user),
            !string.IsNullOrEmpty(await userManager.GetAuthenticatorKeyAsync(user)));
    }

    public async Task<string[]?> GenerateRecoveryCodesAsync(string userId)
    {
        using var scope = scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return null;
        }

        var recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, RecoveryCodesCount);
        return recoveryCodes?.ToArray();
    }
}
