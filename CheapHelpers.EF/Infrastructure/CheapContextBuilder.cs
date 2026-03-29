using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.EF.Infrastructure
{
    /// <summary>
    /// Builder for fluent CheapContext configuration.
    /// Works with all context levels: CheapContext, CheapCommunicationContext, CheapBusinessContext.
    /// Identity stores are registered against CheapContext (the base), which EF resolves correctly
    /// regardless of which derived context is actually registered in DI.
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
        /// Identity stores are registered against the base CheapContext, which works for all context levels
        /// because CheapCommunicationContext and CheapBusinessContext both derive from CheapContext.
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
    // Identity-only context:
    // services.AddCheapContext<MyUser>(options => options.UseSqlServer(connectionString))
    //     .AddIdentity<IdentityRole>();
    //
    // Communication context (Identity + notifications, preferences, file attachments):
    // services.AddCheapCommunicationContext<MyUser>(options => options.UseSqlServer(connectionString))
    //     .AddIdentity<IdentityRole>();
    //
    // Business context (Communication + API keys, billing, reporting):
    // services.AddCheapBusinessContext<MyUser>(options => options.UseSqlServer(connectionString))
    //     .AddIdentity<IdentityRole>();
}