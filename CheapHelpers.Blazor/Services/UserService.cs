using CheapHelpers;
using CheapHelpers.EF;
using CheapHelpers.EF.Repositories;
using CheapHelpers.Models.Entities;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace CheapHelpers.Blazor.Services
{
    /// <summary>
    /// Generic Blazor user service replacing UserManager/RoleManager.
    /// All auth-state methods validate authentication before proceeding.
    /// </summary>
    /// <typeparam name="TUser">Concrete user type extending CheapUser</typeparam>
    public class UserService<TUser>(IDbContextFactory<CheapContext<TUser>> factory) : UserRepo<TUser>(factory)
        where TUser : CheapUser
    {
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

        public async Task<TUser> GetUserAsync(ClaimsPrincipal principal, CheapContext<TUser>? context = null)
        {
            string id = GetUserId(principal);
            return await GetUserAsync(id, context);
        }

        public async Task<TUser> GetUserAsync(Task<AuthenticationState> authstate, CheapContext<TUser>? context = null)
        {
            var r = await authstate;
            return await GetUserAsync(r, context);
        }

        public async Task<TUser> GetUserAsync(string userId, CheapContext<TUser>? context = null)
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

        public async Task<TUser> GetUserAsync(AuthenticationState authstate, CheapContext<TUser>? context = null)
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

        public static string GetUserId(ClaimsPrincipal principal)
        {
            if (!IsAuthenticated(principal))
            {
                throw new InvalidOperationException(Constants.Authentication.UserNotAuthenticatedMessage);
            }

            return principal.FindFirstValue(ClaimTypes.NameIdentifier);
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
    }

    /// <summary>
    /// Backward-compatible UserService hardcoded to CheapUser.
    /// New consumers should use <see cref="UserService{TUser}"/> with their concrete user type.
    /// </summary>
    public class UserService(IDbContextFactory<CheapContext<CheapUser>> factory) : UserService<CheapUser>(factory);
}
