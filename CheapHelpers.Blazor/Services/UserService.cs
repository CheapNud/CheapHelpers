using CheapHelpers;
using CheapHelpers.EF;
using CheapHelpers.EF.Repositories;
using CheapHelpers.Models.Entities;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Security.Claims;

namespace CheapHelpers.Blazor.Services
{
    /// <summary>
    /// Generic Blazor user service replacing UserManager/RoleManager.
    /// All auth-state methods validate authentication before proceeding.
    /// Also implements <see cref="IUserService"/>, the non-generic seam the shared Account pages inject.
    /// </summary>
    /// <typeparam name="TUser">Concrete user type extending CheapUser</typeparam>
    public class UserService<TUser, TContext>(
        IDbContextFactory<TContext> factory,
        IPasswordHasher<TUser> passwordHasher,
        ILookupNormalizer normalizer,
        IOptions<IdentityOptions> identityOptions,
        IdentityErrorDescriber errors,
        IServiceScopeFactory scopeFactory)
        : UserRepo<TUser, TContext>(factory, passwordHasher, normalizer, identityOptions, errors), IUserService
        where TUser : CheapUser
        where TContext : CheapContext<TUser>
    {
        private const int RecoveryCodesCount = 10;

        #region Navigation State Management

        public async Task<bool> UpdateNavigationStateAsync(Task<AuthenticationState> authState, string navigationKey, bool expanded, CancellationToken token = default)
        {
            var userId = await GetUserIdAsync(authState);
            return await UpdateNavigationStateAsync(userId, navigationKey, expanded, token);
        }

        public async Task<bool> UpdateNavigationStateAsync(string userId, string navigationKey, bool expanded, CancellationToken token = default)
        {
            try
            {
                Debug.WriteLine($"Updating navigation state for user {userId}: {navigationKey} = {expanded}");

                using var context = _factory.CreateDbContext();
                var user = await context.Users.FirstOrDefaultAsync(x => x.Id == userId, token);

                if (user == null)
                {
                    Debug.WriteLine($"User with ID {userId} not found");
                    return false;
                }

                user.SetExpandState(navigationKey, expanded);

                await context.SaveChangesAsync(token);
                Debug.WriteLine($"Navigation state updated successfully for user {userId}");

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating navigation state: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateNavigationStatesAsync(string userId, Dictionary<string, bool> navigationStates, CancellationToken token = default)
        {
            try
            {
                Debug.WriteLine($"Batch updating navigation states for user {userId}: {navigationStates.Count} states");

                using var context = _factory.CreateDbContext();
                var user = await context.Users.FirstOrDefaultAsync(x => x.Id == userId, token);

                if (user == null)
                {
                    Debug.WriteLine($"User with ID {userId} not found");
                    return false;
                }

                foreach (var kvp in navigationStates)
                {
                    user.SetExpandState(kvp.Key, kvp.Value);
                }

                await context.SaveChangesAsync(token);
                Debug.WriteLine($"Batch navigation state update completed for user {userId}");

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error batch updating navigation states: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> GetNavigationStateAsync(string userId, string navigationKey, CancellationToken token = default)
        {
            try
            {
                using var context = _factory.CreateDbContext();
                var user = await context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == userId, token);

                return user?.GetExpandState(navigationKey) ?? false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting navigation state: {ex.Message}");
                return false;
            }
        }

        public async Task<Dictionary<string, bool>> GetAllNavigationStatesAsync(string userId, CancellationToken token = default)
        {
            try
            {
                using var context = _factory.CreateDbContext();
                var user = await context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == userId, token);

                return user?.GetAllExpandStates() ?? new Dictionary<string, bool>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting all navigation states: {ex.Message}");
                return new Dictionary<string, bool>();
            }
        }

        #endregion

        public async Task<TUser> GetUserAsync(ClaimsPrincipal principal, TContext? context = null)
        {
            string id = GetUserId(principal);
            return await GetUserAsync(id, context);
        }

        public async Task<TUser> GetUserAsync(Task<AuthenticationState> authstate, TContext? context = null)
        {
            var r = await authstate;
            return await GetUserAsync(r, context);
        }

        public async Task<TUser> GetUserAsync(string userId, TContext? context = null)
        {
            if (context == null)
            {
                using var tcontext = _factory.CreateDbContext();
                return await tcontext.Users.AsNoTracking().FirstAsync(x => x.Id == userId);
            }
            else
            {
                return await context.Users.FirstAsync(x => x.Id == userId);
            }
        }

        public async Task<TUser> GetUserAsync(AuthenticationState authstate, TContext? context = null)
        {
            return await GetUserAsync(authstate.User, context);
        }

        public async Task<string> GetUserIdAsync(Task<AuthenticationState> authstate)
        {
            var r = await authstate;
            return GetUserId(r.User);
        }

        public static bool IsInRole(ClaimsPrincipal principal, string role)
        {
            if (!IsAuthenticated(principal))
            {
                throw new InvalidOperationException(Constants.Authentication.UserNotAuthenticatedMessage);
            }

            return principal.IsInRole(role);
        }

        /// <summary>
        /// Reads the user id from the claim type Identity is configured with.
        /// </summary>
        public string GetUserId(ClaimsPrincipal principal)
        {
            if (!IsAuthenticated(principal))
            {
                throw new InvalidOperationException(Constants.Authentication.UserNotAuthenticatedMessage);
            }

            return principal.FindFirstValue(IdentityOptions.ClaimsIdentity.UserIdClaimType);
        }

        public static string GetUserName(ClaimsPrincipal principal)
        {
            if (!IsAuthenticated(principal))
            {
                throw new InvalidOperationException(Constants.Authentication.UserNotAuthenticatedMessage);
            }

            return principal.FindFirstValue(ClaimTypes.Name);
        }

        public static bool IsAuthenticated(ClaimsPrincipal principal)
        {
            ArgumentNullException.ThrowIfNull(principal);

            return
                    principal.Identities != null
                    && principal.Identities.Any(
                        i => i.AuthenticationType == IdentityConstants.ApplicationScheme
                    )
                 || principal.Identity != null && principal.Identity.IsAuthenticated;
        }

        public static async Task<bool> IsAuthenticated(Task<AuthenticationState> authtask)
        {
            var r = await authtask;
            return IsAuthenticated(r.User);
        }

        public static bool IsAuthenticated(AuthenticationState auth)
        {
            return IsAuthenticated(auth.User);
        }

        #region Account pages (IUserService)

        // The token operations below (password reset, authenticator, two-factor, recovery codes, email tokens,
        // external logins) need Identity's token providers, which only UserManager offers. They resolve
        // UserManager from a fresh DI scope per call, so they are safe from Blazor Server circuits.
        // Everything else goes through UserRepo, which has no UserManager.

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

        // Explicit: the public GetUserAsync(principal) above returns TUser and throws for anonymous
        // principals, while the pages want null. Reads the id from the claim type Identity is configured with
        async Task<CheapUser?> IUserService.GetUserAsync(ClaimsPrincipal principal)
        {
            var userId = principal.FindFirstValue(IdentityOptions.ClaimsIdentity.UserIdClaimType);
            return userId is null ? null : await GetUserByIdAsync(userId);
        }

        public async Task<CheapUser?> FindByIdAsync(string userId) => await GetUserByIdAsync(userId);

        public async Task<CheapUser?> FindByEmailAsync(string email) => await GetUserByEmailAsync(email);

        // UserRepo's profile updater rather than UserManager.UpdateAsync: the pages hold detached,
        // possibly stale users, and UpdateAsync would write their stale Identity columns back or
        // fail on the concurrency stamp. Explicit because UserRepo.UpdateUserAsync(TUser) returns bool
        async Task<IdentityResult> IUserService.UpdateUserAsync(CheapUser user, CancellationToken token)
            => await UpdateUserAsync(AsTUser(user), token)
                ? IdentityResult.Success
                : UserNotFound(user.Id);

        // Explicit so it doesn't compete with UserRepo.CreateUserAsync(TUser, string?) on this type
        Task<IdentityResult> IUserService.CreateUserAsync(CheapUser user, string password)
            => CreateUserAsync(AsTUser(user), password);

        public async Task<string> GenerateEmailConfirmationTokenAsync(string userId)
        {
            using var scope = scopeFactory.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();
            return await userManager.GenerateEmailConfirmationTokenAsync(await RequireUserAsync(userManager, userId));
        }

        // UserRepo.ConfirmEmailAsync only sets the flag, so the code is checked first with VerifyUserTokenAsync
        public async Task<IdentityResult> ConfirmEmailAsync(string userId, string code)
        {
            var user = await GetUserByIdAsync(userId);
            if (user is null)
            {
                return UserNotFound(userId);
            }

            var tokenProvider = IdentityOptions.Tokens.EmailConfirmationTokenProvider;
            if (!await VerifyUserTokenAsync(user, tokenProvider, UserManager<TUser>.ConfirmEmailTokenPurpose, code))
            {
                return IdentityResult.Failed(new IdentityError { Code = "InvalidToken", Description = "Invalid token." });
            }

            return await ConfirmEmailAsync(user) ? IdentityResult.Success : UserNotFound(userId);
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

            var changeResult = await userManager.ChangeEmailAsync(user, newEmail, code);
            if (!changeResult.Succeeded || !userNameWasEmail)
            {
                return changeResult;
            }

            return await userManager.SetUserNameAsync(user, newEmail);
        }

        public async Task<bool> IsEmailConfirmedAsync(string userId)
            => (await GetUserByIdAsync(userId))?.EmailConfirmed == true;

        public async Task<string> GeneratePasswordResetTokenAsync(string userId)
            => await GeneratePasswordResetTokenAsync(await GetUserByIdAsync(userId)
                ?? throw new InvalidOperationException($"User '{userId}' not found."));

        public async Task<IdentityResult> ResetPasswordAsync(string userId, string code, string newPassword)
        {
            var user = await GetUserByIdAsync(userId);
            return user is null ? UserNotFound(userId) : await ResetPasswordAsync(user, code, newPassword);
        }

        public async Task<bool> HasPasswordAsync(string userId)
            => (await GetUserByIdAsync(userId))?.PasswordHash is not null;

        /// <summary>
        /// Generates a password reset token for the user in an isolated DI scope.
        /// </summary>
        public async Task<string> GeneratePasswordResetTokenAsync(TUser user)
        {
            using var scope = scopeFactory.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();
            return await userManager.GeneratePasswordResetTokenAsync(await RequireUserAsync(userManager, user.Id));
        }

        /// <summary>
        /// Verifies a user token for the given provider and purpose in an isolated DI scope.
        /// </summary>
        public async Task<bool> VerifyUserTokenAsync(TUser user, string tokenProvider, string purpose, string verificationToken)
        {
            using var scope = scopeFactory.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();
            return await userManager.VerifyUserTokenAsync(await RequireUserAsync(userManager, user.Id), tokenProvider, purpose, verificationToken);
        }

        /// <summary>
        /// Resets the password with a reset token: the token is checked with <see cref="VerifyUserTokenAsync"/>
        /// (InvalidToken when it fails, like UserManager.ResetPasswordAsync), then UserRepo sets the password.
        /// That gives ConcurrencyFailure when the user changed after <paramref name="user"/> was loaded.
        /// </summary>
        public async Task<IdentityResult> ResetPasswordAsync(TUser user, string resetToken, string newPassword)
            => await VerifyUserTokenAsync(user, IdentityOptions.Tokens.PasswordResetTokenProvider, UserManager<TUser>.ResetPasswordTokenPurpose, resetToken)
                ? await SetPasswordAsync(user, newPassword)
                : IdentityResult.Failed(ErrorDescriber.InvalidToken());

        private static async Task<TUser> RequireUserAsync(UserManager<TUser> userManager, string userId) =>
            await userManager.FindByIdAsync(userId)
                ?? throw new InvalidOperationException($"User '{userId}' not found.");

        private static TUser AsTUser(CheapUser user) =>
            user as TUser ?? throw new InvalidCastException(
                $"The account pages got a {user.GetType().Name}, but the app's user type is {typeof(TUser).Name}. " +
                $"Make {nameof(Configuration.CheapHelpersBlazorOptions)}.{nameof(Configuration.CheapHelpersBlazorOptions.CreateUser)} return {typeof(TUser).Name} instances.");

        #endregion
    }
}
