using CheapHelpers.EF.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
            services.TryAddSingleton(options);

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
        public static CheapContextBuilder<TUser, CheapCommunicationContext<TUser>> AddCheapCommunicationContext<TUser>(
            this IServiceCollection services,
            Action<DbContextOptionsBuilder> configureContext,
            CheapContextOptions? contextOptions = null)
            where TUser : IdentityUser
        {
            var options = contextOptions ?? new CheapContextOptions();
            services.TryAddSingleton(options);

            services.AddDbContext<CheapCommunicationContext<TUser>>(configureContext);

            // Forward scoped + factory registrations so services requiring base types resolve correctly
            services.AddScoped<CheapContext<TUser>>(sp => sp.GetRequiredService<CheapCommunicationContext<TUser>>());
            services.AddSingleton<IDbContextFactory<CheapContext<TUser>>>(sp =>
                new DbContextFactoryAdapter<CheapContext<TUser>, CheapCommunicationContext<TUser>>(
                    sp.GetRequiredService<IDbContextFactory<CheapCommunicationContext<TUser>>>()));

            return new CheapContextBuilder<TUser, CheapCommunicationContext<TUser>>(services, options);
        }

        /// <summary>
        /// Adds CheapBusinessContext (Identity + communications + API keys, billing, reporting).
        /// Chain .AddIdentity() for Identity services.
        /// </summary>
        public static CheapContextBuilder<TUser, CheapBusinessContext<TUser>> AddCheapBusinessContext<TUser>(
            this IServiceCollection services,
            Action<DbContextOptionsBuilder> configureContext,
            CheapContextOptions? contextOptions = null)
            where TUser : IdentityUser
        {
            var options = contextOptions ?? new CheapContextOptions();
            services.TryAddSingleton(options);

            services.AddDbContext<CheapBusinessContext<TUser>>(configureContext);

            // Forward scoped + factory registrations for both base types
            services.AddScoped<CheapCommunicationContext<TUser>>(sp => sp.GetRequiredService<CheapBusinessContext<TUser>>());
            services.AddScoped<CheapContext<TUser>>(sp => sp.GetRequiredService<CheapBusinessContext<TUser>>());
            services.AddSingleton<IDbContextFactory<CheapCommunicationContext<TUser>>>(sp =>
                new DbContextFactoryAdapter<CheapCommunicationContext<TUser>, CheapBusinessContext<TUser>>(
                    sp.GetRequiredService<IDbContextFactory<CheapBusinessContext<TUser>>>()));
            services.AddSingleton<IDbContextFactory<CheapContext<TUser>>>(sp =>
                new DbContextFactoryAdapter<CheapContext<TUser>, CheapBusinessContext<TUser>>(
                    sp.GetRequiredService<IDbContextFactory<CheapBusinessContext<TUser>>>()));

            return new CheapContextBuilder<TUser, CheapBusinessContext<TUser>>(services, options);
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