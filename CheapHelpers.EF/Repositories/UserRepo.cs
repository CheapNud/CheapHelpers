using CheapHelpers.EF.Extensions;
using CheapHelpers.EF.Infrastructure;
using CheapHelpers.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Linq.Expressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace CheapHelpers.EF.Repositories
{
    /// <summary>
    /// Enhanced repository for user operations, replacing basic UserManager/RoleManager functionality
    /// Optimized for Blazor Server scenarios where HttpContext is not available
    /// </summary>
    public class UserRepo(IDbContextFactory<CheapContext<CheapUser>> factory) : BaseRepo(factory)
    {
        private const int DEFAULT_USER_PAGE_SIZE = 20;

        #region Events

        public event Action RefreshNotifications = delegate { };

        public void OnChatRead()
        {
            RefreshNotifications.Invoke();
        }

        #endregion

        #region Role Management

        /// <summary>
        /// Removes a user from a specified role
        /// </summary>
        public async Task<bool> RemoveFromRoleAsync(CheapUser user, string roleName, CancellationToken token = default)
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
        public async Task<bool> AddToRoleAsync(CheapUser user, string roleName, CancellationToken token = default)
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
        public async Task<int> AddUsersToRoleAsync(IEnumerable<CheapUser> users, string roleName, CancellationToken token = default)
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
        public async Task<List<CheapUser>> GetUsersInRoleAsync(string roleName, CancellationToken token = default)
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
        public async Task<PaginatedList<CheapUser>> GetUsersInRolePaginatedAsync(string roleName, int? pageIndex = null, int pageSize = DEFAULT_USER_PAGE_SIZE, CancellationToken token = default)
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

                return await PaginatedList<CheapUser>.CreateAsync(query, pageIndex.Value, pageSize, token);
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
        public async Task<List<IdentityUserRole<string>>> GetUserRolesAsync(CheapUser user, CancellationToken token = default)
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
        public async Task<List<string>> GetUserRoleNamesAsync(CheapUser user, CancellationToken token = default)
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
        public async Task<bool> IsInRoleAsync(CheapUser user, string roleName, CancellationToken token = default)
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
        public async Task<bool> SetUserRolesAsync(CheapUser user, IEnumerable<string> roleNames, CancellationToken token = default)
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
        /// Updates user information with proper change tracking
        /// </summary>
        public async Task<bool> UpdateUserAsync(CheapUser user, CancellationToken token = default)
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

                // Update user properties - uncomment and modify as needed
                trackedUser.FirstName = user.FirstName;
                trackedUser.LastName = user.LastName;
                trackedUser.PhoneNumber = user.PhoneNumber;
                trackedUser.TimeZoneInfoId = user.TimeZoneInfoId;
                trackedUser.IsDarkMode = user.IsDarkMode;
                trackedUser.PinCodeHash = user.PinCodeHash;

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
        public async Task<bool> UpdateUserPropertiesAsync<TProperty>(CheapUser user, Expression<Func<CheapUser, TProperty>> propertySelector, TProperty newValue, CancellationToken token = default)
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
                    var property = typeof(CheapUser).GetProperty(memberExpression.Member.Name);
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
        public async Task<bool> SetEmailAsync(CheapUser user, string email, CancellationToken token = default)
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

                trackedUser.Email = email;
                trackedUser.NormalizedEmail = email.ToUpperInvariant();
                trackedUser.EmailConfirmed = false;

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
        public async Task<bool> ConfirmEmailAsync(CheapUser user, CancellationToken token = default)
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
        /// Gets user by email address
        /// </summary>
        public async Task<CheapUser?> GetUserByEmailAsync(string email, CancellationToken token = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(email);

            try
            {
                using var context = _factory.CreateDbContext();
                return await context.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email == email, token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting user by email: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets user by username
        /// </summary>
        public async Task<CheapUser?> GetUserByUsernameAsync(string username, CancellationToken token = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(username);

            try
            {
                using var context = _factory.CreateDbContext();
                return await context.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserName == username, token);
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
        public async Task<List<CheapUser>> SearchUsersAsync(string searchTerm, CancellationToken token = default)
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
        public async Task<PaginatedList<CheapUser>> SearchUsersPaginatedAsync(string searchTerm, int? pageIndex = null, int pageSize = DEFAULT_USER_PAGE_SIZE, CancellationToken token = default)
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

                return await PaginatedList<CheapUser>.CreateAsync(query, pageIndex.Value, pageSize, token);
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
        public async Task<PaginatedList<CheapUser>> GetAllUsersPaginatedAsync(int? pageIndex = null, int pageSize = DEFAULT_USER_PAGE_SIZE, CancellationToken token = default)
        {
            using var context = _factory.CreateDbContext();
            var query = _factory.CreateDbContext().Users.AsNoTracking();

            return await PaginatedList<CheapUser>.CreateAsync(query, pageIndex.Value, pageSize, token);
        }

        /// <summary>
        /// Deletes a user and all associated data
        /// </summary>
        public async Task<bool> DeleteUserAsync(CheapUser user, CancellationToken token = default)
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