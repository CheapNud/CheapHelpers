using CheapHelpers.EF.Infrastructure;
using CheapHelpers.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq.Expressions;

namespace CheapHelpers.EF.Repositories
{
    /// <summary>
    /// Enhanced repository for user operations, replacing the basic Identity user and role manager functionality
    /// Optimized for Blazor Server scenarios where HttpContext is not available
    /// </summary>
    public class UserRepo<TUser, TContext>(
        IDbContextFactory<TContext> factory,
        IPasswordHasher<TUser> passwordHasher,
        ILookupNormalizer normalizer,
        IOptions<IdentityOptions> identityOptions,
        IdentityErrorDescriber errors,
        IServiceScopeFactory scopeFactory) : BaseRepo<TContext>(factory)
        where TUser : CheapUser
        where TContext : CheapContext<TUser>
    {
        private const int DEFAULT_USER_PAGE_SIZE = 20;

        #region Events

        public event Action RefreshNotifications = delegate { };

        public void OnChatRead()
        {
            RefreshNotifications.Invoke();
        }

        #endregion

        #region Identity Operations

        // These run on EF with Identity's password hasher, lookup normalizer, options and error describer, so
        // UserRepo holds no Identity user manager. Token operations (password reset, email, 2FA, recovery codes)
        // need Identity's token providers and live in UserService.

        /// <summary>
        /// Identity options the app registered (password rules, user rules, lockout, claim types).
        /// </summary>
        protected IdentityOptions IdentityOptions => identityOptions.Value;

        /// <summary>
        /// Identity's error describer, for results that match Identity's error codes.
        /// </summary>
        protected IdentityErrorDescriber ErrorDescriber => errors;

        /// <summary>
        /// Creates a user, hashing the password when given. Same checks and order as Identity's
        /// CreateAsync: password rules, then user name and email rules.
        /// </summary>
        public async Task<IdentityResult> CreateUserAsync(TUser user, string? password = null, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(user);

            if (password is not null)
            {
                var passwordResult = SetPasswordHash(user, password);
                if (!passwordResult.Succeeded)
                {
                    return passwordResult;
                }
            }

            user.SecurityStamp = NewSecurityStamp();

            using var context = _factory.CreateDbContext();

            var userResult = await ValidateUserAsync(context, user, token);
            if (!userResult.Succeeded)
            {
                return userResult;
            }

            if (IdentityOptions.Lockout.AllowedForNewUsers)
            {
                user.LockoutEnabled = true;
            }

            user.NormalizedUserName = normalizer.NormalizeName(user.UserName);
            user.NormalizedEmail = normalizer.NormalizeEmail(user.Email);

            context.Users.Add(user);
            await context.SaveChangesAsync(token);
            return IdentityResult.Success;
        }

        /// <summary>
        /// Changes the password after checking the current one. A wrong current password gives PasswordMismatch.
        /// </summary>
        public Task<IdentityResult> ChangePasswordAsync(TUser user, string currentPassword, string newPassword, CancellationToken token = default)
            => UpdateStoredUserAsync(user, storedUser =>
                // SuccessRehashNeeded counts as a match; the new hash replaces the old one anyway
                storedUser.PasswordHash is null
                || passwordHasher.VerifyHashedPassword(storedUser, storedUser.PasswordHash, currentPassword) == PasswordVerificationResult.Failed
                    ? IdentityResult.Failed(errors.PasswordMismatch())
                    : SetPasswordHash(storedUser, newPassword), token);

        /// <summary>
        /// Adds a password to a user without one. Gives UserAlreadyHasPassword when one is set.
        /// </summary>
        public Task<IdentityResult> AddPasswordAsync(TUser user, string newPassword, CancellationToken token = default)
            => UpdateStoredUserAsync(user, storedUser => storedUser.PasswordHash is not null
                ? IdentityResult.Failed(errors.UserAlreadyHasPassword())
                : SetPasswordHash(storedUser, newPassword), token);

        /// <summary>
        /// Sets the password without checking the current one. Only for flows that verified the user
        /// another way first, such as a password reset token.
        /// </summary>
        public Task<IdentityResult> SetPasswordAsync(TUser user, string newPassword, CancellationToken token = default)
            => UpdateStoredUserAsync(user, storedUser => SetPasswordHash(storedUser, newPassword), token);

        /// <summary>
        /// Whether the stored user has a password.
        /// </summary>
        public async Task<bool> HasPasswordAsync(TUser user, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(user);

            using var context = _factory.CreateDbContext();
            return await context.Users.AnyAsync(u => u.Id == user.Id && u.PasswordHash != null, token);
        }

        /// <summary>
        /// Sets the lockout end date. Gives UserLockoutNotEnabled when lockout is off for the user.
        /// A date in the past unlocks the user.
        /// </summary>
        public Task<IdentityResult> SetLockoutEndDateAsync(TUser user, DateTimeOffset? lockoutEnd, CancellationToken token = default)
            => UpdateStoredUserAsync(user, storedUser =>
            {
                if (!storedUser.LockoutEnabled)
                {
                    return IdentityResult.Failed(errors.UserLockoutNotEnabled());
                }

                storedUser.LockoutEnd = lockoutEnd;
                return IdentityResult.Success;
            }, token);

        /// <summary>
        /// Same rule as Identity's IsLockedOutAsync: locked out while lockout is enabled and the end date has not passed.
        /// </summary>
        public async Task<bool> IsLockedOutAsync(TUser user, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(user);

            using var context = _factory.CreateDbContext();
            var lockout = await context.Users.AsNoTracking()
                .Where(u => u.Id == user.Id)
                .Select(u => new { u.LockoutEnabled, u.LockoutEnd })
                .FirstOrDefaultAsync(token);

            // Compared here, not in SQL: SQLite cannot translate DateTimeOffset comparisons
            return lockout is { LockoutEnabled: true } && lockout.LockoutEnd >= DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Loads a tracked copy of the user, applies the change and saves it with a new ConcurrencyStamp,
        /// like the EF user store does. Callers' detached or stale instances never reach the context.
        /// </summary>
        private async Task<IdentityResult> UpdateStoredUserAsync(TUser user, Func<TUser, IdentityResult> change, CancellationToken token)
        {
            ArgumentNullException.ThrowIfNull(user);

            using var context = _factory.CreateDbContext();
            var storedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id, token)
                ?? throw new InvalidOperationException($"User '{user.Id}' not found");

            var changeResult = change(storedUser);
            if (!changeResult.Succeeded)
            {
                return changeResult;
            }

            storedUser.ConcurrencyStamp = Guid.NewGuid().ToString();

            try
            {
                await context.SaveChangesAsync(token);
            }
            catch (DbUpdateConcurrencyException)
            {
                return IdentityResult.Failed(errors.ConcurrencyFailure());
            }

            return IdentityResult.Success;
        }

        /// <summary>
        /// Validates the password, then hashes it and renews the security stamp, like Identity's UpdatePasswordHash.
        /// </summary>
        private IdentityResult SetPasswordHash(TUser user, string password)
        {
            var passwordResult = ValidatePassword(password);
            if (!passwordResult.Succeeded)
            {
                return passwordResult;
            }

            user.PasswordHash = passwordHasher.HashPassword(user, password);
            user.SecurityStamp = NewSecurityStamp();
            return IdentityResult.Success;
        }

        /// <summary>
        /// Identity's PasswordValidator rules from <see cref="IdentityOptions.Password"/>, same checks and error order.
        /// </summary>
        private IdentityResult ValidatePassword(string? password)
        {
            var options = IdentityOptions.Password;
            password ??= string.Empty;
            List<IdentityError> passwordErrors = [];

            if (string.IsNullOrWhiteSpace(password) || password.Length < options.RequiredLength)
            {
                passwordErrors.Add(errors.PasswordTooShort(options.RequiredLength));
            }

            // ASCII only, like PasswordValidator: an accented letter counts as non-alphanumeric
            if (options.RequireNonAlphanumeric && password.All(char.IsAsciiLetterOrDigit))
            {
                passwordErrors.Add(errors.PasswordRequiresNonAlphanumeric());
            }

            if (options.RequireDigit && !password.Any(char.IsAsciiDigit))
            {
                passwordErrors.Add(errors.PasswordRequiresDigit());
            }

            if (options.RequireLowercase && !password.Any(char.IsAsciiLetterLower))
            {
                passwordErrors.Add(errors.PasswordRequiresLower());
            }

            if (options.RequireUppercase && !password.Any(char.IsAsciiLetterUpper))
            {
                passwordErrors.Add(errors.PasswordRequiresUpper());
            }

            if (options.RequiredUniqueChars >= 1 && password.Distinct().Count() < options.RequiredUniqueChars)
            {
                passwordErrors.Add(errors.PasswordRequiresUniqueChars(options.RequiredUniqueChars));
            }

            return passwordErrors.Count > 0 ? IdentityResult.Failed([.. passwordErrors]) : IdentityResult.Success;
        }

        /// <summary>
        /// Identity's UserValidator rules from <see cref="IdentityOptions.User"/>: user name, then email
        /// when <see cref="UserOptions.RequireUniqueEmail"/> is on. Duplicates are found on the normalized columns.
        /// </summary>
        private async Task<IdentityResult> ValidateUserAsync(TContext context, TUser user, CancellationToken token)
        {
            var options = IdentityOptions.User;
            List<IdentityError> userErrors = [];

            var userName = user.UserName;
            if (string.IsNullOrWhiteSpace(userName)
                || (!string.IsNullOrEmpty(options.AllowedUserNameCharacters) && userName.Any(c => !options.AllowedUserNameCharacters.Contains(c))))
            {
                userErrors.Add(errors.InvalidUserName(userName));
            }
            else
            {
                var normalizedName = normalizer.NormalizeName(userName);
                if (await context.Users.AnyAsync(u => u.NormalizedUserName == normalizedName && u.Id != user.Id, token))
                {
                    userErrors.Add(errors.DuplicateUserName(userName));
                }
            }

            if (options.RequireUniqueEmail)
            {
                var email = user.Email;
                if (string.IsNullOrWhiteSpace(email) || !new EmailAddressAttribute().IsValid(email))
                {
                    userErrors.Add(errors.InvalidEmail(email));
                }
                else
                {
                    var normalizedEmail = normalizer.NormalizeEmail(email);
                    if (await context.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail && u.Id != user.Id, token))
                    {
                        userErrors.Add(errors.DuplicateEmail(email));
                    }
                }
            }

            return userErrors.Count > 0 ? IdentityResult.Failed([.. userErrors]) : IdentityResult.Success;
        }

        // Opaque value: Identity only compares it and feeds it to the token providers
        private static string NewSecurityStamp() => Guid.NewGuid().ToString("N");

        /// <summary>
        /// Generates a password reset token for the user in an isolated DI scope.
        /// </summary>
        public Task<string> GeneratePasswordResetTokenAsync(TUser user)
            => WithUserManagerAsync(user, static (userManager, scopedUser) =>
                userManager.GeneratePasswordResetTokenAsync(scopedUser));

        /// <summary>
        /// Verifies a user token for the given provider and purpose in an isolated DI scope.
        /// </summary>
        public Task<bool> VerifyUserTokenAsync(TUser user, string tokenProvider, string purpose, string verificationToken)
            => WithUserManagerAsync(user, (userManager, scopedUser) =>
                userManager.VerifyUserTokenAsync(scopedUser, tokenProvider, purpose, verificationToken));

        /// <summary>
        /// Resets the user's password with a reset token in an isolated DI scope.
        /// </summary>
        public Task<IdentityResult> ResetPasswordAsync(TUser user, string resetToken, string newPassword)
            => WithUserManagerAsync(user, (userManager, scopedUser) =>
                userManager.ResetPasswordAsync(scopedUser, resetToken, newPassword));

        /// <summary>
        /// Runs a UserManager operation in a fresh DI scope against a freshly loaded user,
        /// so detached entities from other contexts never leak into Identity's store.
        /// </summary>
        private async Task<TResult> WithUserManagerAsync<TResult>(TUser user, Func<UserManager<TUser>, TUser, Task<TResult>> operation)
        {
            ArgumentNullException.ThrowIfNull(user);

            using var scope = scopeFactory.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();

            var scopedUser = await userManager.FindByIdAsync(user.Id)
                ?? throw new InvalidOperationException($"User '{user.Id}' not found");

            return await operation(userManager, scopedUser);
        }

        #endregion

        #region Role Management

        /// <summary>
        /// Removes a user from a specified role
        /// </summary>
        public async Task<bool> RemoveFromRoleAsync(TUser user, string roleName, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(user);
            ArgumentException.ThrowIfNullOrWhiteSpace(roleName);

            try
            {
                using var context = _factory.CreateDbContext();

                var role = await context.Roles.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Name == roleName, token);

                if (role == null)
                {
                    Debug.WriteLine($"Role '{roleName}' not found");
                    return false;
                }

                var userRole = await context.UserRoles
                    .FirstOrDefaultAsync(x => x.RoleId == role.Id && x.UserId == user.Id, token);

                if (userRole == null)
                {
                    Debug.WriteLine($"User '{user.Id}' is not in role '{roleName}'");
                    return false;
                }

                context.UserRoles.Remove(userRole);
                await context.SaveChangesAsync(token);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error removing user from role: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Adds a user to a specified role
        /// </summary>
        public async Task<bool> AddToRoleAsync(TUser user, string roleName, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(user);
            ArgumentException.ThrowIfNullOrWhiteSpace(roleName);

            try
            {
                using var context = _factory.CreateDbContext();

                var role = await context.Roles.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Name == roleName, token);

                if (role == null)
                {
                    Debug.WriteLine($"Role '{roleName}' not found");
                    return false;
                }

                // Check if user is already in role
                var existingUserRole = await context.UserRoles.AsNoTracking()
                    .AnyAsync(x => x.RoleId == role.Id && x.UserId == user.Id, token);

                if (existingUserRole)
                {
                    Debug.WriteLine($"User '{user.Id}' is already in role '{roleName}'");
                    return true;
                }

                context.UserRoles.Add(new IdentityUserRole<string>
                {
                    RoleId = role.Id,
                    UserId = user.Id
                });

                await context.SaveChangesAsync(token);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding user to role: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Adds multiple users to a role efficiently
        /// </summary>
        public async Task<int> AddUsersToRoleAsync(IEnumerable<TUser> users, string roleName, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(users);
            ArgumentException.ThrowIfNullOrWhiteSpace(roleName);

            var userList = users.ToList();
            if (userList.Count == 0)
                return 0;

            try
            {
                using var context = _factory.CreateDbContext();

                var role = await context.Roles.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Name == roleName, token);

                if (role == null)
                {
                    Debug.WriteLine($"Role '{roleName}' not found");
                    return 0;
                }

                var userIds = userList.Select(u => u.Id).ToList();
                var existingUserRoles = await context.UserRoles.AsNoTracking()
                    .Where(ur => ur.RoleId == role.Id && userIds.Contains(ur.UserId))
                    .Select(ur => ur.UserId)
                    .ToListAsync(token);

                var newUserRoles = userList
                    .Where(u => !existingUserRoles.Contains(u.Id))
                    .Select(u => new IdentityUserRole<string>
                    {
                        RoleId = role.Id,
                        UserId = u.Id
                    })
                    .ToList();

                if (newUserRoles.Count > 0)
                {
                    context.UserRoles.AddRange(newUserRoles);
                    await context.SaveChangesAsync(token);
                }

                return newUserRoles.Count;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding users to role: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets all users in a specified role with optimized query
        /// </summary>
        public async Task<List<TUser>> GetUsersInRoleAsync(string roleName, CancellationToken token = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(roleName);

            try
            {
                using var context = _factory.CreateDbContext();

                return await context.Users.AsNoTracking()
                    .Join(context.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => new { User = u, ur.RoleId })
                    .Join(context.Roles, uur => uur.RoleId, r => r.Id, (uur, r) => new { uur.User, r.Name })
                    .Where(x => x.Name == roleName)
                    .Select(x => x.User)
                    .ToListAsync(token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting users in role: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets paginated users in a specified role
        /// </summary>
        public async Task<PaginatedList<TUser>> GetUsersInRolePaginatedAsync(string roleName, int? pageIndex = null, int pageSize = DEFAULT_USER_PAGE_SIZE, CancellationToken token = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(roleName);

            try
            {
                using var context = _factory.CreateDbContext();

                var query = context.Users.AsNoTracking()
                    .Join(context.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => new { User = u, ur.RoleId })
                    .Join(context.Roles, uur => uur.RoleId, r => r.Id, (uur, r) => new { uur.User, r.Name })
                    .Where(x => x.Name == roleName)
                    .Select(x => x.User);

                return await PaginatedList<TUser>.CreateAsync(query, pageIndex ?? 1, pageSize, token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting paginated users in role: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets user roles with optimized single query
        /// </summary>
        public async Task<List<IdentityUserRole<string>>> GetUserRolesAsync(TUser user, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(user);

            try
            {
                using var context = _factory.CreateDbContext();
                return await context.UserRoles.AsNoTracking()
                    .Where(x => x.UserId == user.Id)
                    .ToListAsync(token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting user roles: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets user role names with optimized single join query
        /// </summary>
        public async Task<List<string>> GetUserRoleNamesAsync(TUser user, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(user);

            try
            {
                using var context = _factory.CreateDbContext();

                return await context.UserRoles.AsNoTracking()
                    .Where(ur => ur.UserId == user.Id)
                    .Join(context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                    .ToListAsync(token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting user role names: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Checks if a user is in a specified role
        /// </summary>
        public async Task<bool> IsInRoleAsync(TUser user, string roleName, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(user);
            ArgumentException.ThrowIfNullOrWhiteSpace(roleName);

            return await IsInRoleAsync(user.Id, roleName, token);
        }

        /// <summary>
        /// Checks if a user ID is in a specified role
        /// </summary>
        public async Task<bool> IsInRoleAsync(string userId, string roleName, CancellationToken token = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);
            ArgumentException.ThrowIfNullOrWhiteSpace(roleName);

            try
            {
                using var context = _factory.CreateDbContext();

                return await context.UserRoles.AsNoTracking()
                    .Join(context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                    .AnyAsync(x => x.UserId == userId && x.Name == roleName, token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking if user is in role: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Replaces all user roles with a new set of roles
        /// </summary>
        public async Task<bool> SetUserRolesAsync(TUser user, IEnumerable<string> roleNames, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(user);
            ArgumentNullException.ThrowIfNull(roleNames);

            var roleList = roleNames.ToList();

            try
            {
                using var context = _factory.CreateDbContext();
                using var transaction = await context.Database.BeginTransactionAsync(token);

                // Remove all existing roles
                var existingUserRoles = await context.UserRoles
                    .Where(ur => ur.UserId == user.Id)
                    .ToListAsync(token);

                if (existingUserRoles.Count > 0)
                {
                    context.UserRoles.RemoveRange(existingUserRoles);
                }

                // Add new roles
                if (roleList.Count > 0)
                {
                    var roles = await context.Roles.AsNoTracking()
                        .Where(r => roleList.Contains(r.Name))
                        .ToListAsync(token);

                    var newUserRoles = roles.Select(r => new IdentityUserRole<string>
                    {
                        UserId = user.Id,
                        RoleId = r.Id
                    }).ToList();

                    context.UserRoles.AddRange(newUserRoles);
                }

                await context.SaveChangesAsync(token);
                await transaction.CommitAsync(token);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting user roles: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region User Management

        /// <summary>
        /// Columns <see cref="UpdateUserAsync"/> never writes: Identity-managed and security-critical
        /// fields. Override in a derived repo to ADD consumer-sensitive columns (tenant ids, roles,
        /// balances, audit fields) — call base and append.
        /// </summary>
        protected virtual IEnumerable<string> ProtectedUserProperties =>
        [
            nameof(CheapUser.UserName), nameof(CheapUser.NormalizedUserName),
            nameof(CheapUser.Email), nameof(CheapUser.NormalizedEmail), nameof(CheapUser.EmailConfirmed),
            nameof(CheapUser.PasswordHash), nameof(CheapUser.SecurityStamp), nameof(CheapUser.ConcurrencyStamp),
            nameof(CheapUser.PhoneNumberConfirmed), nameof(CheapUser.TwoFactorEnabled),
            nameof(CheapUser.LockoutEnd), nameof(CheapUser.LockoutEnabled), nameof(CheapUser.AccessFailedCount),
        ];

        /// <summary>
        /// Updates user profile information: persists ALL scalar properties of <typeparamref name="TUser"/>
        /// except <see cref="ProtectedUserProperties"/>.
        /// <para>
        /// SECURITY: do not pass entities model-bound directly from untrusted input — construct or load
        /// the entity in server code. Derived repos with additional sensitive columns must extend
        /// <see cref="ProtectedUserProperties"/>.
        /// </para>
        /// </summary>
        public async Task<bool> UpdateUserAsync(TUser user, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(user);

            try
            {
                using var context = _factory.CreateDbContext();
                var trackedUser = await context.Users.FirstOrDefaultAsync(x => x.Id == user.Id, token);

                if (trackedUser == null)
                {
                    Debug.WriteLine($"User with ID {user.Id} not found");
                    return false;
                }

                // Copy ALL scalar properties (including consumer-derived ones — a hardcoded
                // list silently dropped them), then restore the Identity-managed and
                // security-critical columns: this is a profile updater, and the incoming
                // entity may be stale on those (identity changes go through their own flows)
                var entry = context.Entry(trackedUser);
                var currentValues = entry.CurrentValues;

                var keptValues = ProtectedUserProperties.ToDictionary(p => p, p => currentValues[p]);

                currentValues.SetValues(user);

                foreach (var kept in keptValues)
                {
                    currentValues[kept.Key] = kept.Value;
                }

                await context.SaveChangesAsync(token);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating user: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Updates specific user properties using a selector
        /// </summary>
        public async Task<bool> UpdateUserPropertiesAsync<TProperty>(TUser user, Expression<Func<TUser, TProperty>> propertySelector, TProperty newValue, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(user);
            ArgumentNullException.ThrowIfNull(propertySelector);

            try
            {
                using var context = _factory.CreateDbContext();
                var trackedUser = await context.Users.FirstOrDefaultAsync(x => x.Id == user.Id, token);

                if (trackedUser == null)
                {
                    Debug.WriteLine($"User with ID {user.Id} not found");
                    return false;
                }

                if (propertySelector.Body is MemberExpression memberExpression)
                {
                    var property = typeof(TUser).GetProperty(memberExpression.Member.Name);
                    property?.SetValue(trackedUser, newValue);

                    await context.SaveChangesAsync(token);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating user property: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Sets user email and normalizes it properly
        /// </summary>
        public async Task<bool> SetEmailAsync(TUser user, string email, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(user);
            ArgumentException.ThrowIfNullOrWhiteSpace(email);

            try
            {
                using var context = _factory.CreateDbContext();
                var trackedUser = await context.Users.FirstOrDefaultAsync(x => x.Id == user.Id, token);

                if (trackedUser == null)
                {
                    Debug.WriteLine($"User with ID {user.Id} not found");
                    return false;
                }

                // Same changes as Identity's SetEmailAsync, plus the ConcurrencyStamp the EF store renews
                trackedUser.Email = email;
                trackedUser.NormalizedEmail = normalizer.NormalizeEmail(email);
                trackedUser.EmailConfirmed = false;
                trackedUser.SecurityStamp = NewSecurityStamp();
                trackedUser.ConcurrencyStamp = Guid.NewGuid().ToString();

                await context.SaveChangesAsync(token);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting user email: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Confirms user email
        /// </summary>
        public async Task<bool> ConfirmEmailAsync(TUser user, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(user);

            try
            {
                using var context = _factory.CreateDbContext();
                var trackedUser = await context.Users.FirstOrDefaultAsync(x => x.Id == user.Id, token);

                if (trackedUser == null)
                {
                    Debug.WriteLine($"User with ID {user.Id} not found");
                    return false;
                }

                trackedUser.EmailConfirmed = true;
                await context.SaveChangesAsync(token);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error confirming user email: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets user by id. Null when missing.
        /// </summary>
        public async Task<TUser?> GetUserByIdAsync(string id, CancellationToken token = default)
        {
            using var context = _factory.CreateDbContext();
            return await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, token);
        }

        /// <summary>
        /// Gets user by email address, on NormalizedEmail like Identity's FindByEmailAsync
        /// </summary>
        public async Task<TUser?> GetUserByEmailAsync(string email, CancellationToken token = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(email);

            try
            {
                using var context = _factory.CreateDbContext();
                var normalizedEmail = normalizer.NormalizeEmail(email);
                return await context.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting user by email: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets user by username, on NormalizedUserName like Identity's FindByNameAsync
        /// </summary>
        public async Task<TUser?> GetUserByUsernameAsync(string username, CancellationToken token = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(username);

            try
            {
                using var context = _factory.CreateDbContext();
                var normalizedName = normalizer.NormalizeName(username);
                return await context.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedName, token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting user by username: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Searches users by name, email, or username
        /// </summary>
        public async Task<List<TUser>> SearchUsersAsync(string searchTerm, CancellationToken token = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(searchTerm);

            try
            {
                using var context = _factory.CreateDbContext();
                var lowerSearchTerm = searchTerm.ToLowerInvariant();

                return await context.Users.AsNoTracking()
                    .Where(u =>
                        u.FirstName.ToLower().Contains(lowerSearchTerm) ||
                        u.LastName.ToLower().Contains(lowerSearchTerm) ||
                        u.Email.ToLower().Contains(lowerSearchTerm) ||
                        u.UserName.ToLower().Contains(lowerSearchTerm))
                    .ToListAsync(token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error searching users: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets paginated search results for users
        /// </summary>
        public async Task<PaginatedList<TUser>> SearchUsersPaginatedAsync(string searchTerm, int? pageIndex = null, int pageSize = DEFAULT_USER_PAGE_SIZE, CancellationToken token = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(searchTerm);

            try
            {
                using var context = _factory.CreateDbContext();
                var lowerSearchTerm = searchTerm.ToLowerInvariant();

                var query = context.Users.AsNoTracking()
                    .Where(u =>
                        u.FirstName.ToLower().Contains(lowerSearchTerm) ||
                        u.LastName.ToLower().Contains(lowerSearchTerm) ||
                        u.Email.ToLower().Contains(lowerSearchTerm) ||
                        u.UserName.ToLower().Contains(lowerSearchTerm));

                return await PaginatedList<TUser>.CreateAsync(query, pageIndex ?? 1, pageSize, token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error searching users paginated: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets all users with pagination
        /// </summary>
        public async Task<PaginatedList<TUser>> GetAllUsersPaginatedAsync(int? pageIndex = null, int pageSize = DEFAULT_USER_PAGE_SIZE, CancellationToken token = default)
        {
            using var context = _factory.CreateDbContext();
            var query = context.Users.AsNoTracking();

            return await PaginatedList<TUser>.CreateAsync(query, pageIndex ?? 1, pageSize, token);
        }

        /// <summary>
        /// Deletes a user and all associated data
        /// </summary>
        public async Task<bool> DeleteUserAsync(TUser user, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(user);

            try
            {
                using var context = _factory.CreateDbContext();
                using var transaction = await context.Database.BeginTransactionAsync(token);

                // Remove user roles first
                var userRoles = await context.UserRoles
                    .Where(ur => ur.UserId == user.Id)
                    .ToListAsync(token);

                if (userRoles.Count > 0)
                {
                    context.UserRoles.RemoveRange(userRoles);
                }

                // Remove the user
                var trackedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id, token);
                if (trackedUser != null)
                {
                    context.Users.Remove(trackedUser);
                }

                await context.SaveChangesAsync(token);
                await transaction.CommitAsync(token);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting user: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Gets user statistics
        /// </summary>
        public async Task<UserStatistics> GetUserStatisticsAsync(CancellationToken token = default)
        {
            try
            {
                using var context = _factory.CreateDbContext();

                var totalUsers = await context.Users.CountAsync(token);
                var confirmedEmails = await context.Users.CountAsync(u => u.EmailConfirmed, token);
                var recentUsers = await context.Users
                    .CountAsync(u => u.LockoutEnd == null || u.LockoutEnd < DateTimeOffset.UtcNow, token);

                return new UserStatistics
                {
                    TotalUsers = totalUsers,
                    ConfirmedEmails = confirmedEmails,
                    ActiveUsers = recentUsers,
                    ConfirmationRate = totalUsers > 0 ? (double)confirmedEmails / totalUsers * 100 : 0
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting user statistics: {ex.Message}");
                throw;
            }
        }

        #endregion
    }
}
