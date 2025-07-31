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
        public static IServiceCollection AddCheapHelpersBlazor<TUser>(
            this IServiceCollection services,
            Action<CheapHelpersBlazorOptions>? configure = null)
            where TUser : CheapUser
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
            services.AddScoped<UserService>();

            // Register email service if configured
            if (options.EmailServiceType != null)
            {
                services.AddScoped(typeof(IEmailService), options.EmailServiceType);
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
            return services.AddCheapHelpersBlazor<CheapUser>(options =>
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
            return services.AddCheapHelpersBlazor<TUser>(options =>
            {
                options.EnableLocalization = false;
                options.EnableFileDownload = false;
            });
        }

        /// <summary>
        /// Complete setup: CheapContext + Blazor services in one call
        /// </summary>
        public static IServiceCollection AddCheapHelpersComplete<TUser>(
            this IServiceCollection services,
            Action<DbContextOptionsBuilder> configureContext,
            CheapContextOptions? contextOptions = null,
            Action<CheapHelpersBlazorOptions>? configureBlazor = null)
            where TUser : CheapUser
        {
            // Add CheapContext
            services.AddCheapContext<TUser>(configureContext, contextOptions);

            // Add Blazor services
            services.AddCheapHelpersBlazor<TUser>(configureBlazor);

            return services;
        }

        /// <summary>
        /// Complete setup with Identity: CheapContext + Identity + Blazor services
        /// </summary>
        public static IServiceCollection AddCheapHelpersCompleteWithIdentity<TUser, TRole>(
            this IServiceCollection services,
            Action<DbContextOptionsBuilder> configureContext,
            CheapContextOptions? contextOptions = null,
            Action<IdentityOptions>? configureIdentity = null,
            Action<CheapHelpersBlazorOptions>? configureBlazor = null)
            where TUser : CheapUser
            where TRole : IdentityRole
        {
            // Add CheapContext + Identity
            services.AddCheapContext<TUser>(configureContext, contextOptions)
                .AddIdentity<TRole>(configureIdentity);

            // Add Blazor services
            services.AddCheapHelpersBlazor<TUser>(configureBlazor);

            return services;
        }

        /// <summary>
        /// Simple complete setup with IdentityUser and IdentityRole
        /// </summary>
        public static IServiceCollection AddCheapHelpersCompleteWithIdentity(
            this IServiceCollection services,
            Action<DbContextOptionsBuilder> configureContext,
            CheapContextOptions? contextOptions = null,
            Action<IdentityOptions>? configureIdentity = null,
            Action<CheapHelpersBlazorOptions>? configureBlazor = null)
        {
            return services.AddCheapHelpersCompleteWithIdentity<CheapUser, IdentityRole>(
                configureContext, contextOptions, configureIdentity, configureBlazor);
        }
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