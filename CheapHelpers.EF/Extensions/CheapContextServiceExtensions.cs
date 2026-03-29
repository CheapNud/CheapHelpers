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
        /// Adds CheapContext (Identity only) with the specified user type. Chain .AddIdentity() for Identity services.
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
        /// Adds CheapContext (Identity only) with IdentityUser. Chain .AddIdentity() for Identity services.
        /// </summary>
        public static CheapContextBuilder<IdentityUser> AddCheapContext(
            this IServiceCollection services,
            Action<DbContextOptionsBuilder> configureContext,
            CheapContextOptions? contextOptions = null)
        {
            return services.AddCheapContext<IdentityUser>(configureContext, contextOptions);
        }

        /// <summary>
        /// Adds CheapCommunicationContext (Identity + notifications, preferences, file attachments).
        /// Chain .AddIdentity() for Identity services.
        /// </summary>
        public static CheapContextBuilder<TUser> AddCheapCommunicationContext<TUser>(
            this IServiceCollection services,
            Action<DbContextOptionsBuilder> configureContext,
            CheapContextOptions? contextOptions = null)
            where TUser : IdentityUser
        {
            var options = contextOptions ?? new CheapContextOptions();

            // Register context options
            services.AddSingleton(options);

            // Add DbContext — register as both the concrete type and the base CheapContext<TUser>
            services.AddDbContext<CheapCommunicationContext<TUser>>(configureContext);
            services.AddScoped<CheapContext<TUser>>(sp => sp.GetRequiredService<CheapCommunicationContext<TUser>>());

            return new CheapContextBuilder<TUser>(services, options);
        }

        /// <summary>
        /// Adds CheapBusinessContext (Identity + communications + API keys, billing, reporting).
        /// Chain .AddIdentity() for Identity services.
        /// </summary>
        public static CheapContextBuilder<TUser> AddCheapBusinessContext<TUser>(
            this IServiceCollection services,
            Action<DbContextOptionsBuilder> configureContext,
            CheapContextOptions? contextOptions = null)
            where TUser : IdentityUser
        {
            var options = contextOptions ?? new CheapContextOptions();

            // Register context options
            services.AddSingleton(options);

            // Add DbContext — register as the concrete type and both base types
            services.AddDbContext<CheapBusinessContext<TUser>>(configureContext);
            services.AddScoped<CheapCommunicationContext<TUser>>(sp => sp.GetRequiredService<CheapBusinessContext<TUser>>());
            services.AddScoped<CheapContext<TUser>>(sp => sp.GetRequiredService<CheapBusinessContext<TUser>>());

            return new CheapContextBuilder<TUser>(services, options);
        }
    }

    // Usage Examples:
    //
    // Identity-only context (users, roles, navigation state):
    // services.AddCheapContext<MyUser>(options => options.UseSqlServer(connectionString));
    //
    // Communication context (Identity + notifications, preferences, file attachments):
    // services.AddCheapCommunicationContext<MyUser>(options => options.UseSqlServer(connectionString))
    //     .AddIdentity<IdentityRole>();
    //
    // Business context (Communication + API keys, billing, reporting):
    // services.AddCheapBusinessContext<MyUser>(options => options.UseSqlServer(connectionString))
    //     .AddIdentity<IdentityRole>();
    //
    // Context + Identity with defaults (most common):
    // services.AddCheapContext<ApplicationUser>(options => options.UseSqlServer(connectionString))
    //     .AddIdentity<IdentityRole>();
    //
    // Simple case with IdentityUser/IdentityRole:
    // services.AddCheapContext(options => options.UseSqlServer(connectionString))
    //     .AddIdentity();
}