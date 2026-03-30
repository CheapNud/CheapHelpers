using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.EF.Infrastructure
{
    /// <summary>
    /// Builder for fluent CheapContext configuration.
    /// Tracks the concrete context type so Identity stores are registered against the correct level.
    /// </summary>
    public class CheapContextBuilder<TUser, TContext>
        where TUser : IdentityUser
        where TContext : DbContext
    {
        private readonly IServiceCollection _services;
        private readonly CheapContextOptions _contextOptions;

        internal CheapContextBuilder(IServiceCollection services, CheapContextOptions contextOptions)
        {
            _services = services;
            _contextOptions = contextOptions;
        }

        /// <summary>
        /// Adds Identity services with CheapContext defaults.
        /// Identity stores are registered against the actual context type (<typeparamref name="TContext"/>),
        /// not the base <c>CheapContext</c>, ensuring correct DI resolution at all context levels.
        /// </summary>
        public IdentityBuilder AddIdentity<TRole>(Action<IdentityOptions>? configureOptions = null)
            where TRole : IdentityRole
        {
            var identityBuilder = _services.AddIdentity<TUser, TRole>(identityOptions =>
            {
                identityOptions.Password = _contextOptions.Identity.Password;
                identityOptions.SignIn = _contextOptions.Identity.SignIn;
                identityOptions.Lockout = _contextOptions.Identity.Lockout;
                identityOptions.User = _contextOptions.Identity.User;
                identityOptions.Stores = _contextOptions.Identity.Stores;
                identityOptions.Tokens = _contextOptions.Identity.Tokens;
                identityOptions.ClaimsIdentity = _contextOptions.Identity.ClaimsIdentity;

                configureOptions?.Invoke(identityOptions);
            })
            .AddEntityFrameworkStores<TContext>()
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

    /// <summary>
    /// Backward-compatible builder defaulting to <c>CheapContext&lt;TUser&gt;</c>.
    /// </summary>
    public class CheapContextBuilder<TUser>(IServiceCollection services, CheapContextOptions contextOptions)
        : CheapContextBuilder<TUser, CheapContext<TUser>>(services, contextOptions)
        where TUser : IdentityUser;
}
