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
    /// Blazor Replacement for UsermManager and RoleManager
    /// All Task<AuthenticationState>, AuthenticationState and Claimsprincipal tasks check for authentication!
    /// </summary>
    public class UserService(IDbContextFactory<CheapContext<CheapUser>> factory) : UserRepo(factory)
    {
        #region Navigation State Management

        /// <summary>
        /// Updates navigation state for the current user based on authentication state
        /// </summary>
        /// <param name="authState">Authentication state</param>
        /// <param name="navigationKey">Key for the navigation section (e.g., "Service", "Production")</param>
        /// <param name="expanded">Whether the section should be expanded</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if update was successful</returns>
        public async Task<bool> UpdateNavigationStateAsync(Task<AuthenticationState> authState, string navigationKey, bool expanded, CancellationToken token = default)
        {
            var userId = await GetUserIdAsync(authState);
            return await UpdateNavigationStateAsync(userId, navigationKey, expanded, token);
        }

        /// <summary>
        /// Updates navigation state for a specific user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="navigationKey">Key for the navigation section</param>
        /// <param name="expanded">Whether the section should be expanded</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if update was successful</returns>
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

                // Update the navigation state using the enhanced method
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

        /// <summary>
        /// Batch update multiple navigation states for better performance
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="navigationStates">Dictionary of navigation keys and their expanded states</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if update was successful</returns>
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

                // Update all navigation states
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

        /// <summary>
        /// Gets navigation state for a specific section
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="navigationKey">Navigation section key</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if expanded, false if collapsed or user not found</returns>
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

        /// <summary>
        /// Gets all navigation states for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Dictionary of all navigation states</returns>
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

        public async Task<CheapUser> GetUserAsync(ClaimsPrincipal principal, CheapContext<CheapUser> context = null)
        {
            string id = GetUserId(principal);
            return await GetUserAsync(id, context);
        }

        public async Task<CheapUser> GetUserAsync(Task<AuthenticationState> authstate, CheapContext<CheapUser> context = null)
        {
            var r = await authstate;
            return await GetUserAsync(r, context);
        }

        public async Task<CheapUser> GetUserAsync(string userId, CheapContext<CheapUser> context = null)
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

        public async Task<CheapUser> GetUserAsync(AuthenticationState authstate, CheapContext<CheapUser> context = null)
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
        /// throws when user not authenticated, otherwise returns id
        /// </summary>
        /// <param name="principal"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static string GetUserId(ClaimsPrincipal principal)
        {
            if (!IsAuthenticated(principal))
            {
                throw new InvalidOperationException(Constants.Authentication.UserNotAuthenticatedMessage);
            }

            return principal.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        /// <summary>
        /// throws when user not authenticated, otherwise returns username (default: mail)
        /// </summary>
        /// <param name="principal"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static string GetUserName(ClaimsPrincipal principal)
        {
            if (!IsAuthenticated(principal))
            {
                throw new InvalidOperationException(Constants.Authentication.UserNotAuthenticatedMessage);
            }

            return principal.FindFirstValue(ClaimTypes.Name);
        }

        /// <summary>
        /// Checks the claimsprincipal for authentication
        /// </summary>
        /// <param name="principal"></param>
        /// <returns></returns>
        public static bool IsAuthenticated(ClaimsPrincipal principal)
        {
            if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal));
            }

            return
                    principal.Identities != null
                    && principal.Identities.Any(
                        i => i.AuthenticationType == IdentityConstants.ApplicationScheme
                    )
                 || principal.Identity != null && principal.Identity.IsAuthenticated;
        }

        /// <summary>
        /// Checks the claimsprincipal for authentication
        /// </summary>
        /// <param name="principal"></param>
        /// <returns></returns>
        public static async Task<bool> IsAuthenticated(Task<AuthenticationState> authtask)
        {
            var r = await authtask;
            return IsAuthenticated(r.User);
        }

        /// <summary>
        /// Checks the claimsprincipal for authentication
        /// </summary>
        /// <param name="principal"></param>
        /// <returns></returns>
        public static bool IsAuthenticated(AuthenticationState auth)
        {
            return IsAuthenticated(auth.User);
        }

    }
}
