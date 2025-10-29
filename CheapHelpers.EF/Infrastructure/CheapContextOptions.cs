using CheapHelpers;
using Microsoft.AspNetCore.Identity;

namespace CheapHelpers.EF.Infrastructure
{
    public class CheapContextOptions
    {
        public string EnvironmentVariable { get; set; } = Constants.Environment.AspNetCoreEnvironment;
        public int DevCommandTimeoutMs { get; set; } = 150000;
        public bool EnableAuditing { get; set; } = true;
        public bool EnableSensitiveDataLogging { get; set; } = true;

        // Use the real IdentityOptions with your defaults
        public IdentityOptions Identity { get; set; } = new()
        {
            Password = new PasswordOptions
            {
                RequireDigit = true,
                RequireLowercase = true,
                RequireNonAlphanumeric = false,
                RequireUppercase = true,
                RequiredLength = 8,
                RequiredUniqueChars = 1
            },
            SignIn = new SignInOptions
            {
                RequireConfirmedAccount = false
            },
            Lockout = new LockoutOptions
            {
                DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5),
                MaxFailedAccessAttempts = 8,
                AllowedForNewUsers = true
            },
            User = new UserOptions
            {
                AllowedUserNameCharacters = Constants.Authentication.AllowedUserNameCharacters,
                RequireUniqueEmail = true
            }
        };
    }
}
