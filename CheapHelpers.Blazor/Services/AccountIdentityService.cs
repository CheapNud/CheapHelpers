using CheapHelpers.EF;
using CheapHelpers.Models.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

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
/// Lockout state for the lockout page.
/// </summary>
public sealed record LockoutInfo(DateTimeOffset? LockoutEnd, bool IsLockedOut);

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

    /// <summary>
    /// Loads the signed-in user. Null when the principal has no user id or the user doesn't exist.
    /// </summary>
    Task<CheapUser?> GetUserAsync(ClaimsPrincipal principal);

    Task<CheapUser?> FindByIdAsync(string userId);

    Task<CheapUser?> FindByEmailAsync(string email);

    /// <summary>
    /// Saves profile fields. Identity-managed columns (password, security stamp, email, lockout)
    /// keep their stored values, see UserRepo.UpdateUserAsync.
    /// </summary>
    Task<IdentityResult> UpdateUserAsync(CheapUser user, CancellationToken token = default);

    /// <summary>
    /// Saves only the given navigation expand states, leaving the rest of the user untouched.
    /// </summary>
    Task<bool> UpdateNavigationStatesAsync(string userId, Dictionary<string, bool> navigationStates, CancellationToken token = default);

    /// <summary>
    /// Creates the user with a password. The user must be the app's user type; get a fresh one from
    /// <see cref="Configuration.CheapHelpersBlazorOptions.CreateUser"/>.
    /// </summary>
    /// <exception cref="InvalidCastException">The user is not the registered user type.</exception>
    Task<IdentityResult> CreateUserAsync(CheapUser user, string password);

    /// <exception cref="InvalidOperationException">The user doesn't exist.</exception>
    Task<string> GenerateEmailConfirmationTokenAsync(string userId);

    Task<IdentityResult> ConfirmEmailAsync(string userId, string code);

    /// <summary>
    /// Changes the email, and the user name too when the user name was the old email.
    /// </summary>
    Task<IdentityResult> ChangeEmailAsync(string userId, string newEmail, string code);

    Task<bool> IsEmailConfirmedAsync(string userId);

    /// <exception cref="InvalidOperationException">The user doesn't exist.</exception>
    Task<string> GeneratePasswordResetTokenAsync(string userId);

    Task<IdentityResult> ResetPasswordAsync(string userId, string code, string newPassword);

    /// <summary>
    /// Lockout state of the user with this email. Null for unknown emails.
    /// </summary>
    Task<LockoutInfo?> GetLockoutAsync(string email);

    Task<bool> HasPasswordAsync(string userId);
}

/// <summary>
/// Scope-per-call implementation: resolves UserManager from a fresh DI scope per operation,
/// same pattern as UserRepo's identity helpers — safe from Blazor Server circuits.
/// </summary>
public class AccountIdentityService<TUser, TContext>(IServiceScopeFactory scopeFactory) : IAccountIdentityService
    where TUser : CheapUser
    where TContext : CheapContext<TUser>
{
    private const int RecoveryCodesCount = 10;

    private static IdentityResult UserNotFound(string userId) =>
        IdentityResult.Failed(new IdentityError { Code = "UserNotFound", Description = $"User '{userId}' not found." });

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

    public async Task<CheapUser?> GetUserAsync(ClaimsPrincipal principal)
    {
        using var scope = scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();
        return await userManager.GetUserAsync(principal);
    }

    public async Task<CheapUser?> FindByIdAsync(string userId)
    {
        using var scope = scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();
        return await userManager.FindByIdAsync(userId);
    }

    public async Task<CheapUser?> FindByEmailAsync(string email)
    {
        using var scope = scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();
        return await userManager.FindByEmailAsync(email);
    }

    // UserRepo's profile updater rather than UserManager.UpdateAsync: the pages hold detached,
    // possibly stale users, and UpdateAsync would write their stale Identity columns back or
    // fail on the concurrency stamp
    public async Task<IdentityResult> UpdateUserAsync(CheapUser user, CancellationToken token = default)
    {
        using var scope = scopeFactory.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<UserService<TUser, TContext>>();
        return await userService.UpdateUserAsync(AsTUser(user), token)
            ? IdentityResult.Success
            : UserNotFound(user.Id);
    }

    public async Task<bool> UpdateNavigationStatesAsync(string userId, Dictionary<string, bool> navigationStates, CancellationToken token = default)
    {
        using var scope = scopeFactory.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<UserService<TUser, TContext>>();
        return await userService.UpdateNavigationStatesAsync(userId, navigationStates, token);
    }

    public async Task<IdentityResult> CreateUserAsync(CheapUser user, string password)
    {
        using var scope = scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();
        return await userManager.CreateAsync(AsTUser(user), password);
    }

    public async Task<string> GenerateEmailConfirmationTokenAsync(string userId)
    {
        using var scope = scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();
        return await userManager.GenerateEmailConfirmationTokenAsync(await RequireUserAsync(userManager, userId));
    }

    public async Task<IdentityResult> ConfirmEmailAsync(string userId, string code)
    {
        using var scope = scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();

        var user = await userManager.FindByIdAsync(userId);
        return user is null ? UserNotFound(userId) : await userManager.ConfirmEmailAsync(user, code);
    }

    public async Task<IdentityResult> ChangeEmailAsync(string userId, string newEmail, string code)
    {
        using var scope = scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return UserNotFound(userId);
        }

        // Compare before the change: afterwards user.Email already holds the new address
        var userNameWasEmail = string.Equals(
            await userManager.GetUserNameAsync(user),
            await userManager.GetEmailAsync(user),
            StringComparison.OrdinalIgnoreCase);

        var result = await userManager.ChangeEmailAsync(user, newEmail, code);
        if (!result.Succeeded || !userNameWasEmail)
        {
            return result;
        }

        return await userManager.SetUserNameAsync(user, newEmail);
    }

    public async Task<bool> IsEmailConfirmedAsync(string userId)
    {
        using var scope = scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();

        var user = await userManager.FindByIdAsync(userId);
        return user is not null && await userManager.IsEmailConfirmedAsync(user);
    }

    public async Task<string> GeneratePasswordResetTokenAsync(string userId)
    {
        using var scope = scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();
        return await userManager.GeneratePasswordResetTokenAsync(await RequireUserAsync(userManager, userId));
    }

    public async Task<IdentityResult> ResetPasswordAsync(string userId, string code, string newPassword)
    {
        using var scope = scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();

        var user = await userManager.FindByIdAsync(userId);
        return user is null ? UserNotFound(userId) : await userManager.ResetPasswordAsync(user, code, newPassword);
    }

    public async Task<LockoutInfo?> GetLockoutAsync(string email)
    {
        using var scope = scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return null;
        }

        return new LockoutInfo(await userManager.GetLockoutEndDateAsync(user), await userManager.IsLockedOutAsync(user));
    }

    public async Task<bool> HasPasswordAsync(string userId)
    {
        using var scope = scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();

        var user = await userManager.FindByIdAsync(userId);
        return user is not null && await userManager.HasPasswordAsync(user);
    }

    private static async Task<TUser> RequireUserAsync(UserManager<TUser> userManager, string userId) =>
        await userManager.FindByIdAsync(userId)
            ?? throw new InvalidOperationException($"User '{userId}' not found.");

    private static TUser AsTUser(CheapUser user) =>
        user as TUser ?? throw new InvalidCastException(
            $"The account pages got a {user.GetType().Name}, but the app's user type is {typeof(TUser).Name}. " +
            $"Make {nameof(Configuration.CheapHelpersBlazorOptions)}.{nameof(Configuration.CheapHelpersBlazorOptions.CreateUser)} return {typeof(TUser).Name} instances.");
}
