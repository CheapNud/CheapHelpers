using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

// Deliberately in the EF namespace: these used to be instance methods on CheapContextBuilder,
// so no new using directive is needed. Parameterless AddIdentity() call sites keep compiling
// (all type args infer from the receiver). BREAKING: the role overload changed from
// AddIdentity<TRole>() to AddIdentity<TUser, TContext, TRole>() — C# cannot partially infer
// generic arguments on extension methods.
namespace CheapHelpers.EF.Infrastructure
{
    /// <summary>
    /// Identity registration for <see cref="CheapContextBuilder{TUser, TContext}"/>.
    /// Lives in CheapHelpers.Blazor because <c>IServiceCollection.AddIdentity</c> requires the
    /// ASP.NET Core shared framework, which CheapHelpers.EF avoids to stay Android-consumable.
    /// </summary>
    public static class CheapContextBuilderIdentityExtensions
    {
        /// <summary>
        /// Adds Identity services with CheapContext defaults.
        /// Identity stores are registered against the actual context type (<typeparamref name="TContext"/>),
        /// not the base <c>CheapContext</c>, ensuring correct DI resolution at all context levels.
        /// </summary>
        public static IdentityBuilder AddIdentity<TUser, TContext, TRole>(
            this CheapContextBuilder<TUser, TContext> builder,
            Action<IdentityOptions>? configureOptions = null)
            where TUser : IdentityUser
            where TContext : DbContext
            where TRole : IdentityRole
        {
            var identityBuilder = builder.Services.AddIdentity<TUser, TRole>(identityOptions =>
            {
                identityOptions.Password = builder.ContextOptions.Identity.Password;
                identityOptions.SignIn = builder.ContextOptions.Identity.SignIn;
                identityOptions.Lockout = builder.ContextOptions.Identity.Lockout;
                identityOptions.User = builder.ContextOptions.Identity.User;
                identityOptions.Stores = builder.ContextOptions.Identity.Stores;
                identityOptions.Tokens = builder.ContextOptions.Identity.Tokens;
                identityOptions.ClaimsIdentity = builder.ContextOptions.Identity.ClaimsIdentity;

                configureOptions?.Invoke(identityOptions);
            })
            .AddEntityFrameworkStores<TContext>()
            .AddDefaultTokenProviders();

            return identityBuilder;
        }

        /// <summary>
        /// Adds Identity services with IdentityRole and CheapContext defaults.
        /// </summary>
        public static IdentityBuilder AddIdentity<TUser, TContext>(
            this CheapContextBuilder<TUser, TContext> builder,
            Action<IdentityOptions>? configureOptions = null)
            where TUser : IdentityUser
            where TContext : DbContext
        {
            return builder.AddIdentity<TUser, TContext, IdentityRole>(configureOptions);
        }
    }
}
