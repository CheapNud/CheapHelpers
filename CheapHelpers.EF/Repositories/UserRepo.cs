using CheapHelpers.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace CheapHelpers.EF.Repositories
{
    /// <summary>
    /// Basic operations normally contained in user/role manager
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="mapper"></param>
    public class UserRepo(IDbContextFactory<CheapContext> factory) : BaseRepo(factory)
    {
        public async Task RemoveFromRoleAsync(CheapUser user, string role)
        {
            using var context = _factory.CreateDbContext();
            var dbrole = await context.Roles.AsNoTracking().FirstAsync(x => x.Name == role);
            var userrole = await context.UserRoles.FirstAsync(
                x => x.RoleId == dbrole.Id && x.UserId == user.Id
            );
            context.UserRoles.Remove(userrole);
            await context.SaveChangesAsync();
        }

        public async Task AddToRoleAsync(CheapUser user, string role)
        {
            using var context = _factory.CreateDbContext();
            var dbrole = await context.Roles.AsNoTracking().FirstAsync(x => x.Name == role);
            context.UserRoles.Add(
                new IdentityUserRole<string> { RoleId = dbrole.Id, UserId = user.Id }
            );
            await context.SaveChangesAsync();
        }

        public async Task<List<CheapUser>> GetUsersInRole(string role)
        {
            using var context = _factory.CreateDbContext();
            var dbrole = await context.Roles.AsNoTracking().FirstAsync(x => x.Name == role);
            var userrole = await context
                .UserRoles.AsNoTracking()
                .Where(x => x.RoleId == dbrole.Id)
                .Select(x => x.UserId)
                .ToArrayAsync();
            return await context
                .Users.AsNoTracking()
                .Where(x => userrole.Any(y => y == x.Id))
                .ToListAsync();
        }

        public async Task<List<IdentityUserRole<string>>> GetUserRoles(CheapUser user)
        {
            using var context = _factory.CreateDbContext();
            return await context
                .UserRoles.AsNoTracking()
                .Where(x => x.UserId == user.Id)
                .ToListAsync();
        }

        public async Task<List<string>> GetUserStringRoles(CheapUser user)
        {
            using var context = _factory.CreateDbContext();
            var r = await context
                .UserRoles.AsNoTracking()
                .Where(x => x.UserId == user.Id)
                .ToListAsync();
            List<string> output = new();
            foreach (var sr in r)
            {
                output.Add(
                    await context.Roles.Where(x => x.Id == sr.RoleId).Select(x => x.Name).FirstAsync()
                );
            }
            return output;
        }

        public async Task<bool> IsInRole(CheapUser user, string role)
        {
            using var context = _factory.CreateDbContext();
            return await IsInRole(user.Id, role);
        }

        public async Task<bool> IsInRole(string userid, string role)
        {
            using var context = _factory.CreateDbContext();
            var dbrole = await context.Roles.AsNoTracking().FirstAsync(x => x.Name == role);
            return await context
                .UserRoles.AsNoTracking()
                .Where(x => x.UserId == userid)
                .Select(x => x.RoleId)
                .ContainsAsync(dbrole.Id);
        }

        public async Task UpdateUser(CheapUser user)
        {
            try
            {
                using var context = _factory.CreateDbContext();
                var trackeduser = await context.Users.FirstAsync(x => x.Id == user.Id);

                //trackeduser.FirstName = user.FirstName;
                //trackeduser.LastName = user.LastName;
                //trackeduser.PhoneNumber = user.PhoneNumber;
                //trackeduser.TimeZoneInfoId = user.TimeZoneInfoId;
                //trackeduser.TotalMileage = user.TotalMileage;
                //trackeduser.ReceiveMailServiceStatus = user.ReceiveMailServiceStatus;
                //trackeduser.ReceiveMailWrongSupply = user.ReceiveMailWrongSupply;
                //trackeduser.SendMailServiceRequest = user.SendMailServiceRequest;
                //trackeduser.IsDarkMode = user.IsDarkMode;
                //trackeduser.ReceiveMailAccountancy = user.ReceiveMailAccountancy;
                //trackeduser.PriceMultiplier = user.PriceMultiplier;
                //trackeduser.PinCodeHash = user.PinCodeHash;

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        public async Task SetEmail(CheapUser user, string email)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (email == null)
            {
                throw new ArgumentNullException(nameof(email));
            }

            using var context = _factory.CreateDbContext();
            var trackeduser = await context.Users.FirstAsync(x => x.Id == user.Id);

            trackeduser.Email = email;
            trackeduser.NormalizedEmail = email.ToUpper();
            trackeduser.EmailConfirmed = false;

            await context.SaveChangesAsync();
        }

        public event Action RefreshNotifications = delegate { };

        public void OnChatRead()
        {
            RefreshNotifications.Invoke();
        }
    }
}
