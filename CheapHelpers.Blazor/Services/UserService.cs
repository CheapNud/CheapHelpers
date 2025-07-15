using CheapHelpers.EF;
using CheapHelpers.EF.Repositories;
using CheapHelpers.Models.Entities;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CheapHelpers.Blazor.Services
{
    /// <summary>
    /// Blazor Replacement for UsermManager and RoleManager
    /// All Task<AuthenticationState>, AuthenticationState and Claimsprincipal tasks check for authentication!
    /// </summary>
    public class UserService(IDbContextFactory<CheapContext<CheapUser>> factory) : UserRepo(factory)
    {
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
                throw new InvalidOperationException("user not authenticated");
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
                throw new InvalidOperationException("user not authenticated");
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
                throw new InvalidOperationException("user not authenticated");
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
