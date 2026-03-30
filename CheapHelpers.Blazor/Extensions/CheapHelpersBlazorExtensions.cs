using CheapHelpers.Blazor.Configuration;
using CheapHelpers.Blazor.Services;
using CheapHelpers.EF;
using CheapHelpers.EF.Extensions;
using CheapHelpers.EF.Infrastructure;
using CheapHelpers.Models.Entities;
using CheapHelpers.Services.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MudBlazor;
using MudBlazor.Services;
using System.Diagnostics;

namespace CheapHelpers.Blazor.Extensions
{
    public static class CheapHelpersBlazorExtensions
    {
        /// <summary>
        /// Register CheapHelpers Blazor services with full configuration
        /// </summary>
        public static IServiceCollection AddCheapHelpersBlazor<TUser, TContext>(
            this IServiceCollection services,
            Action<CheapHelpersBlazorOptions>? configure = null)
            where TUser : CheapUser
            where TContext : CheapContext<TUser>
        {
            // Configure options
            var options = new CheapHelpersBlazorOptions();
            configure?.Invoke(options);
            services.AddSingleton(options);

            // Add MudBlazor if not already registered, kind of a hack to detect mudblazor presence
            if (services.Any(x => x.ServiceType == typeof(IMudPopoverHolder)))
            {
                Debug.WriteLine($"Warning: {nameof(IMudPopoverHolder)} already registered, manage state properly! continuing...");
            }

            if (services.Any(x => x.ServiceType == typeof(IDialogService)))
            {
                Debug.WriteLine($"Warning: {nameof(IDialogService)} already registered, manage state properly! continuing...");
            }

            if (services.Any(x => x.ServiceType == typeof(ISnackbar)))
            {
                Debug.WriteLine($"Warning: {nameof(ISnackbar)} already registered, manage state properly! continuing...");
            }

            services.AddMudServices(config =>
                {
                    config.SnackbarConfiguration.PositionClass = options.SnackbarPosition;
                    config.SnackbarConfiguration.PreventDuplicates = options.PreventDuplicateSnackbars;
                    config.SnackbarConfiguration.NewestOnTop = false;
                    config.SnackbarConfiguration.ShowCloseIcon = true;
                    config.SnackbarConfiguration.MaxDisplayedSnackbars = options.MaxSnackbars;
                    config.SnackbarConfiguration.VisibleStateDuration = options.SnackbarDuration;
                    config.SnackbarConfiguration.HideTransitionDuration = 300;
                    config.SnackbarConfiguration.ShowTransitionDuration = 300;
                    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
                });


            // Add localization support
            if (options.EnableLocalization)
            {
                services.AddLocalization();
            }

            // Register core services
            services.AddScoped<UserService<TUser, TContext>>();

            // Register account route options (configurable via CheapHelpersBlazorOptions)
            if (options.AccountRouteOptions is not null)
                services.AddSingleton(options.AccountRouteOptions);
            else
                services.AddSingleton(new Pages.Account.AccountRouteOptions());

            // Register email service — real provider or NullEmailService fallback
            if (options.EmailServiceType != null)
            {
                services.AddScoped(typeof(IEmailService), options.EmailServiceType);
            }
            else
            {
                services.AddSingleton<IEmailService, NullEmailService>();
            }

            // Register DbContext factory if not already registered
            if (!services.Any(x => x.ServiceType == typeof(IDbContextFactory<CheapContext<TUser>>)))
            {
                Debug.WriteLine($"Warning: IDbContextFactory<CheapContext<{typeof(TUser).Name}>> not found. Make sure to register it in your Program.cs");
            }

            // Register additional services based on options
            if (options.EnableFileDownload)
            {
                // Add file download service if available
                // services.AddBlazorDownloadFile(); // Uncomment if using BlazorDownloadFile package
            }

            // Register custom services
            foreach (var customService in options.CustomServices)
            {
                services.Add(customService);
            }

            return services;
        }

        /// <summary>
        /// Register CheapHelpers Blazor with minimal configuration (for quick setup with CheapUser)
        /// </summary>
        public static IServiceCollection AddCheapHelpersBlazorMinimal(
            this IServiceCollection services)
        {
            return services.AddCheapHelpersBlazor<CheapUser, CheapContext<CheapUser>>(options =>
            {
                options.EnableLocalization = false;
                options.EnableFileDownload = false;
            });
        }

        /// <summary>
        /// Register CheapHelpers Blazor with minimal configuration for custom user type
        /// </summary>
        public static IServiceCollection AddCheapHelpersBlazorMinimal<TUser>(
            this IServiceCollection services)
            where TUser : CheapUser
        {
            return services.AddCheapHelpersBlazor<TUser, CheapContext<TUser>>(options =>
            {
                options.EnableLocalization = false;
                options.EnableFileDownload = false;
            });
        }

        /// <summary>
        /// Complete setup with derived context: registers context + Blazor services in one call.
        /// <code>services.AddCheapHelpersComplete&lt;VoltiqUser, VoltiqDbContext&gt;(opts => opts.UseSqlServer(conn));</code>
        /// </summary>
        public static IServiceCollection AddCheapHelpersComplete<TUser, TContext>(
            this IServiceCollection services,
            Action<DbContextOptionsBuilder> configureContext,
            CheapContextOptions? contextOptions = null,
            Action<CheapHelpersBlazorOptions>? configureBlazor = null)
            where TUser : CheapUser
            where TContext : CheapContext<TUser>
        {
            var options = contextOptions ?? new CheapContextOptions();
            services.TryAddSingleton(options);

            services.AddDbContextFactory<TContext>(configureContext);

            // AddDbContextFactory does NOT register TContext as scoped — add it so
            // scoped consumers (controllers, services) can resolve TContext directly.
            services.TryAddScoped(sp => sp.GetRequiredService<IDbContextFactory<TContext>>().CreateDbContext());

            // Forward base-type scoped registrations so services injecting CheapContext<TUser> resolve correctly
            services.AddScoped(sp => (CheapContext<TUser>)sp.GetRequiredService<TContext>());

            // Forward base-type factory registrations via adapter (IDbContextFactory is not covariant)
            services.TryAddSingleton<IDbContextFactory<CheapContext<TUser>>>(sp =>
                new DbContextFactoryAdapter<CheapContext<TUser>, TContext>(
                    sp.GetRequiredService<IDbContextFactory<TContext>>()));

            // If TContext derives from CheapCommunicationContext, forward that level too
            if (typeof(CheapCommunicationContext<TUser>).IsAssignableFrom(typeof(TContext)))
            {
                services.TryAddScoped<CheapCommunicationContext<TUser>>(sp =>
                    (CheapCommunicationContext<TUser>)(object)sp.GetRequiredService<TContext>());
                services.TryAddSingleton<IDbContextFactory<CheapCommunicationContext<TUser>>>(sp =>
                    new CommunicationContextFactoryAdapter<TUser, TContext>(
                        sp.GetRequiredService<IDbContextFactory<TContext>>()));
            }

            services.AddCheapHelpersBlazor<TUser, TContext>(configureBlazor);

            return services;
        }

        /// <summary>
        /// Complete setup with Identity and derived context.
        /// </summary>
        public static IServiceCollection AddCheapHelpersCompleteWithIdentity<TUser, TContext, TRole>(
            this IServiceCollection services,
            Action<DbContextOptionsBuilder> configureContext,
            CheapContextOptions? contextOptions = null,
            Action<IdentityOptions>? configureIdentity = null,
            Action<CheapHelpersBlazorOptions>? configureBlazor = null)
            where TUser : CheapUser
            where TContext : CheapContext<TUser>
            where TRole : IdentityRole
        {
            var options = contextOptions ?? new CheapContextOptions();
            services.TryAddSingleton(options);

            services.AddDbContextFactory<TContext>(configureContext);

            // AddDbContextFactory does NOT register TContext as scoped — add it so
            // scoped consumers (controllers, services) can resolve TContext directly.
            services.TryAddScoped(sp => sp.GetRequiredService<IDbContextFactory<TContext>>().CreateDbContext());

            // Forward base-type scoped registrations so services injecting CheapContext<TUser> resolve correctly
            services.AddScoped(sp => (CheapContext<TUser>)sp.GetRequiredService<TContext>());

            // Forward base-type factory registrations via adapter (IDbContextFactory is not covariant)
            services.TryAddSingleton<IDbContextFactory<CheapContext<TUser>>>(sp =>
                new DbContextFactoryAdapter<CheapContext<TUser>, TContext>(
                    sp.GetRequiredService<IDbContextFactory<TContext>>()));

            // If TContext derives from CheapCommunicationContext, forward that level too
            if (typeof(CheapCommunicationContext<TUser>).IsAssignableFrom(typeof(TContext)))
            {
                services.TryAddScoped<CheapCommunicationContext<TUser>>(sp =>
                    (CheapCommunicationContext<TUser>)(object)sp.GetRequiredService<TContext>());
                services.TryAddSingleton<IDbContextFactory<CheapCommunicationContext<TUser>>>(sp =>
                    new CommunicationContextFactoryAdapter<TUser, TContext>(
                        sp.GetRequiredService<IDbContextFactory<TContext>>()));
            }

            services.AddIdentity<TUser, TRole>(identityOptions =>
            {
                identityOptions.Password = options.Identity.Password;
                identityOptions.SignIn = options.Identity.SignIn;
                identityOptions.Lockout = options.Identity.Lockout;
                identityOptions.User = options.Identity.User;
                configureIdentity?.Invoke(identityOptions);
            }).AddEntityFrameworkStores<TContext>()
              .AddDefaultTokenProviders();

            services.AddCheapHelpersBlazor<TUser, TContext>(configureBlazor);

            return services;
        }

        /// <summary>
        /// Simple complete setup with CheapUser, CheapContext, and IdentityRole defaults.
        /// </summary>
        public static IServiceCollection AddCheapHelpersCompleteWithIdentity(
            this IServiceCollection services,
            Action<DbContextOptionsBuilder> configureContext,
            CheapContextOptions? contextOptions = null,
            Action<IdentityOptions>? configureIdentity = null,
            Action<CheapHelpersBlazorOptions>? configureBlazor = null)
        {
            return services.AddCheapHelpersCompleteWithIdentity<CheapUser, CheapContext<CheapUser>, IdentityRole>(
                configureContext, contextOptions, configureIdentity, configureBlazor);
        }
    }

    /// <summary>
    /// Factory adapter that bridges <c>IDbContextFactory&lt;TContext&gt;</c> to
    /// <c>IDbContextFactory&lt;CheapCommunicationContext&lt;TUser&gt;&gt;</c> via runtime cast.
    /// Required because <c>TContext : CheapContext&lt;TUser&gt;</c> does not statically
    /// prove <c>TContext : CheapCommunicationContext&lt;TUser&gt;</c> — the check is done at registration time.
    /// </summary>
    internal class CommunicationContextFactoryAdapter<TUser, TContext>(IDbContextFactory<TContext> inner)
        : IDbContextFactory<CheapCommunicationContext<TUser>>
        where TUser : CheapUser
        where TContext : CheapContext<TUser>
    {
        public CheapCommunicationContext<TUser> CreateDbContext() =>
            (CheapCommunicationContext<TUser>)(object)inner.CreateDbContext();
    }

    // Usage Examples:
    // 
    // Just Blazor services:
    // services.AddCheapHelpersBlazorMinimal<ApplicationUser>();
    // 
    // Complete setup (Context + Blazor):
    // services.AddCheapHelpersComplete<ApplicationUser>(options => options.UseSqlServer(connectionString));
    // 
    // Everything (Context + Identity + Blazor):
    // services.AddCheapHelpersCompleteWithIdentity<ApplicationUser, IdentityRole>(
    //     options => options.UseSqlServer(connectionString));
    // 
    // Simple everything with defaults:
    // services.AddCheapHelpersCompleteWithIdentity(options => options.UseSqlServer(connectionString));
}