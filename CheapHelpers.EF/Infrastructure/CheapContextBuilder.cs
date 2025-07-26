using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.EF.Infrastructure
{
    /// <summary>
    /// Builder for fluent CheapContext configuration
    /// </summary>
    public class CheapContextBuilder<TUser> where TUser : IdentityUser
    {
        private readonly IServiceCollection _services;
        private readonly CheapContextOptions _contextOptions;

        internal CheapContextBuilder(IServiceCollection services, CheapContextOptions contextOptions)
        {
            _services = services;
            _contextOptions = contextOptions;
        }

        /// <summary>
        /// Adds Identity services with CheapContext defaults. Follows standard .AddIdentity() pattern.
        /// </summary>
        public IdentityBuilder AddIdentity<TRole>(Action<IdentityOptions>? configureOptions = null)
            where TRole : IdentityRole
        {
            var identityBuilder = _services.AddIdentity<TUser, TRole>(identityOptions =>
            {
                // Apply CheapContext defaults first
                identityOptions.Password = _contextOptions.Identity.Password;
                identityOptions.SignIn = _contextOptions.Identity.SignIn;
                identityOptions.Lockout = _contextOptions.Identity.Lockout;
                identityOptions.User = _contextOptions.Identity.User;
                identityOptions.Stores = _contextOptions.Identity.Stores;
                identityOptions.Tokens = _contextOptions.Identity.Tokens;
                identityOptions.ClaimsIdentity = _contextOptions.Identity.ClaimsIdentity;

                // Allow user overrides
                configureOptions?.Invoke(identityOptions);
            })
            .AddEntityFrameworkStores<CheapContext<TUser>>()
            .AddDefaultTokenProviders();

            return identityBuilder;
        }

        /// <summary>
        /// Adds Identity services with IdentityRole and CheapContext defaults.
        /// </summary>
        public IdentityBuilder AddIdentity(Action<IdentityOptions>? configureOptions = null)
        {
            return AddIdentity<IdentityRole>(configureOptions);
        }

        /// <summary>
        /// Access to the underlying service collection for additional configuration.
        /// </summary>
        public IServiceCollection Services => _services;
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