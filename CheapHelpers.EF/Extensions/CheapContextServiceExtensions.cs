using CheapHelpers.EF.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.EF.Extensions
{
    // Extension methods for easy setup
    public static class CheapContextServiceExtensions
    {
        /// <summary>
        /// Adds CheapContext with the specified user type. Chain .AddIdentity() for Identity services.
        /// </summary>
        public static CheapContextBuilder<TUser> AddCheapContext<TUser>(
            this IServiceCollection services,
            Action<DbContextOptionsBuilder> configureContext,
            CheapContextOptions? contextOptions = null)
            where TUser : IdentityUser
        {
            var options = contextOptions ?? new CheapContextOptions();

            // Register context options
            services.AddSingleton(options);

            // Add DbContext
            services.AddDbContext<CheapContext<TUser>>(configureContext);

            return new CheapContextBuilder<TUser>(services, options);
        }

        /// <summary>
        /// Adds CheapContext with IdentityUser. Chain .AddIdentity() for Identity services.
        /// </summary>
        public static CheapContextBuilder<IdentityUser> AddCheapContext(
            this IServiceCollection services,
            Action<DbContextOptionsBuilder> configureContext,
            CheapContextOptions? contextOptions = null)
        {
            return services.AddCheapContext<IdentityUser>(configureContext, contextOptions);
        }
    }

    // Usage Examples:
    // 
    // Context only (no Identity):
    // services.AddCheapContext<MyUser>(options => options.UseSqlServer(connectionString));
    // 
    // Context + Identity with defaults (most common):
    // services.AddCheapContext<ApplicationUser>(options => options.UseSqlServer(connectionString))
    //     .AddIdentity<IdentityRole>();
    // 
    // Context + Identity with custom configuration:
    // services.AddCheapContext<ApplicationUser>(options => options.UseSqlServer(connectionString))
    //     .AddIdentity<IdentityRole>(options => 
    //     {
    //         options.Password.RequiredLength = 12;
    //         options.Lockout.MaxFailedAccessAttempts = 3;
    //     });
    // 
    // Full fluent chain (like Microsoft's pattern):
    // services.AddCheapContext<ApplicationUser>(options => options.UseSqlServer(connectionString))
    //     .AddIdentity<IdentityRole>(options => 
    //     {
    //         options.Password.RequiredLength = 12;
    //     })
    //     .AddDefaultUI()
    //     .AddDefaultTokenProviders()
    //     .Services  // Access underlying IServiceCollection
    //     .AddScoped<IMyService, MyService>();
    // 
    // Simple case with IdentityUser/IdentityRole:
    // services.AddCheapContext(options => options.UseSqlServer(connectionString))
    //     .AddIdentity();
    // 
    // With custom CheapContextOptions:
    // var contextOptions = new CheapContextOptions 
    // {
    //     DevCommandTimeoutMs = 300000,
    //     Identity = new IdentityOptions 
    //     {
    //         Password = new PasswordOptions { RequiredLength = 12 }
    //     }
    // };
    // services.AddCheapContext<ApplicationUser>(options => options.UseSqlServer(connectionString), contextOptions)
    //     .AddIdentity<IdentityRole>();
}