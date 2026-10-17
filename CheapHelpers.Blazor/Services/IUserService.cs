using CheapHelpers.Models.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
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
/// Non-generic identity operations for the shared Account pages. Pages can't be generic over
/// TUser (the router needs concrete types), so they inject this interface and the DI
/// registration supplies <see cref="UserService{TUser, TContext}"/> for the app's user type.
/// </summary>
public interface IUserService
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

    Task<bool> HasPasswordAsync(string userId);

    /// <summary>
    /// Gets the user's passkeys. Null when the user doesn't exist.
    /// </summary>
    Task<IList<UserPasskeyInfo>?> GetPasskeysAsync(string userId);

    /// <summary>
    /// Gets one of the user's passkeys. Null when the user or the passkey doesn't exist.
    /// </summary>
    Task<UserPasskeyInfo?> GetPasskeyAsync(string userId, byte[] credentialId);

    /// <summary>
    /// Adds the passkey, or saves it when the user already has one with the same credential id.
    /// Load the passkey with <see cref="GetPasskeyAsync"/> right before a rename: saving an old copy
    /// writes its old sign count back.
    /// </summary>
    Task<IdentityResult> AddOrUpdatePasskeyAsync(string userId, UserPasskeyInfo passkey);

    Task<IdentityResult> RemovePasskeyAsync(string userId, byte[] credentialId);

    /// <summary>
    /// Verifies a new passkey from the browser against the challenge the PasskeyCreationOptions endpoint issued.
    /// That challenge lives in a cookie, so call this inside an HTTP request (the AddPasskey endpoint does),
    /// never from a Blazor circuit.
    /// </summary>
    /// <exception cref="InvalidOperationException">No passkey registration is underway for this request.</exception>
    Task<PasskeyAttestationResult> PerformPasskeyAttestationAsync(string credentialJson);
}
