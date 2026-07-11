using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.EF.Infrastructure
{
    /// <summary>
    /// Builder for fluent CheapContext configuration.
    /// Tracks the concrete context type so Identity stores are registered against the correct level.
    /// Identity registration (<c>AddIdentity</c>) lives in CheapHelpers.Blazor — it needs the
    /// ASP.NET Core shared framework, which this package deliberately avoids so it stays
    /// consumable from MAUI/Android targets.
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
        /// Access to the underlying service collection for additional configuration.
        /// </summary>
        public IServiceCollection Services => _services;

        /// <summary>
        /// The context options this builder was created with, so extensions can apply the configured defaults.
        /// </summary>
        public CheapContextOptions ContextOptions => _contextOptions;
    }

    /// <summary>
    /// Backward-compatible builder defaulting to <c>CheapContext&lt;TUser&gt;</c>.
    /// </summary>
    public class CheapContextBuilder<TUser>(IServiceCollection services, CheapContextOptions contextOptions)
        : CheapContextBuilder<TUser, CheapContext<TUser>>(services, contextOptions)
        where TUser : IdentityUser;
}
