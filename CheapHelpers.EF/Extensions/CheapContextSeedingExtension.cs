using CheapHelpers.EF.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.EF.Extensions
{

    public static class CheapContextSeedingExtensions
    {
        /// <summary>
        /// Seeds roles and optionally creates a default admin user. Call this after app.Build() but before app.Run().
        /// </summary>
        public static async Task<IServiceProvider> SeedCheapContextAsync<TUser, TRole>(
            this IServiceProvider serviceProvider,
            CheapSeedOptions? options = null)
            where TUser : IdentityUser, new()
            where TRole : IdentityRole, new()
        {
            var seedOptions = options ?? new CheapSeedOptions();

            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetService<ILogger<CheapSeedOptions>>();
            var configuration = services.GetRequiredService<IConfiguration>();

            try
            {
                if (seedOptions.SeedRoles)
                {
                    await SeedRolesAsync<TRole>(services, seedOptions, logger);
                }

                if (seedOptions.CreateDefaultAdmin)
                {
                    await CreateDefaultAdminAsync<TUser, TRole>(services, configuration, seedOptions, logger);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "An error occurred while seeding the database");
                throw;
            }

            return serviceProvider;
        }

        /// <summary>
        /// Convenience method for standard IdentityUser/IdentityRole
        /// </summary>
        public static async Task<IServiceProvider> SeedCheapContextAsync(
            this IServiceProvider serviceProvider,
            CheapSeedOptions? options = null)
        {
            return await serviceProvider.SeedCheapContextAsync<IdentityUser, IdentityRole>(options);
        }

        private static async Task SeedRolesAsync<TRole>(
            IServiceProvider services,
            CheapSeedOptions options,
            ILogger? logger)
            where TRole : IdentityRole, new()
        {
            var roleManager = services.GetRequiredService<RoleManager<TRole>>();

            logger?.LogInformation("Seeding roles: {Roles}", string.Join(", ", options.DefaultRoles));

            foreach (var roleName in options.DefaultRoles)
            {
                var roleExists = await roleManager.RoleExistsAsync(roleName);
                if (!roleExists)
                {
                    var role = new TRole { Name = roleName };
                    var result = await roleManager.CreateAsync(role);

                    if (result.Succeeded)
                    {
                        logger?.LogInformation("Created role: {RoleName}", roleName);
                    }
                    else
                    {
                        logger?.LogWarning("Failed to create role {RoleName}: {Errors}",
                            roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    logger?.LogDebug("Role {RoleName} already exists", roleName);
                }
            }
        }

        private static async Task CreateDefaultAdminAsync<TUser, TRole>(
            IServiceProvider services,
            IConfiguration configuration,
            CheapSeedOptions options,
            ILogger? logger)
            where TUser : IdentityUser, new()
            where TRole : IdentityRole
        {
            var userManager = services.GetRequiredService<UserManager<TUser>>();

            var adminEmail = configuration[options.AdminEmailConfigKey];
            var adminPassword = configuration[options.AdminPasswordConfigKey];

            if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
            {
                logger?.LogWarning("Admin email or password not configured. Skipping admin user creation. " +
                    "Set {EmailKey} and {PasswordKey} in configuration",
                    options.AdminEmailConfigKey, options.AdminPasswordConfigKey);
                return;
            }

            var existingUser = await userManager.FindByEmailAsync(adminEmail);
            if (existingUser != null)
            {
                logger?.LogDebug("Admin user {Email} already exists", adminEmail);
                return;
            }

            var adminUser = new TUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true  // Auto-confirm admin email
            };

            var createResult = await userManager.CreateAsync(adminUser, adminPassword);
            if (createResult.Succeeded)
            {
                logger?.LogInformation("Created admin user: {Email}", adminEmail);

                var addToRoleResult = await userManager.AddToRoleAsync(adminUser, options.AdminRoleName);
                if (addToRoleResult.Succeeded)
                {
                    logger?.LogInformation("Added admin user to {Role} role", options.AdminRoleName);
                }
                else
                {
                    logger?.LogWarning("Failed to add admin user to role {Role}: {Errors}",
                        options.AdminRoleName, string.Join(", ", addToRoleResult.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                logger?.LogError("Failed to create admin user {Email}: {Errors}",
                    adminEmail, string.Join(", ", createResult.Errors.Select(e => e.Description)));
            }
        }
    }
}

// Usage Examples:
//
// Program.cs:
// var builder = WebApplication.CreateBuilder(args);
// 
// builder.Services.AddCheapContext<ApplicationUser>(options => options.UseSqlServer(connectionString))
//     .AddIdentity<IdentityRole>();
// 
// var app = builder.Build();
// 
// // Seed database
// await app.Services.SeedCheapContextAsync<ApplicationUser, IdentityRole>(new CheapSeedOptions
// {
//     DefaultRoles = ["Admin", "Manager", "User"],
//     AdminRoleName = "Admin"
// });
// 
// app.Run();
//
// Or simple version:
// await app.Services.SeedCheapContextAsync(); // Uses defaults
//
// appsettings.json:
// {
//   "UserEmail": "admin@yourapp.com",
//   "UserPassword": "Admin123!",
//   "ConnectionStrings": {
//     "DefaultConnection": "..."
//   }
// }