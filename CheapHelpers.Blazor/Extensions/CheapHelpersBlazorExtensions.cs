using CheapHelpers.Blazor.Configuration;
using CheapHelpers.EF;
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

            // Add MudBlazor if not already registered
            if (!services.Any(x => x.ServiceType == typeof(IMudPopoverService)))
            {
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
            }

            // Add localization support
            if (options.EnableLocalization)
            {
                services.AddLocalization();
            }

            // Register core services
            services.AddScoped<ICheapUserService<TUser>, CheapUserService<TUser, TContext>>();

            // Register email service if configured
            if (options.EmailServiceType != null)
            {
                services.AddScoped(typeof(ICheapEmailService), options.EmailServiceType);
            }

            // Register DbContext factory if not already registered
            if (!services.Any(x => x.ServiceType == typeof(IDbContextFactory<TContext>)))
            {
                Debug.WriteLine("Warning: IDbContextFactory<TContext> not found. Make sure to register it in your Program.cs");
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
        /// Register CheapHelpers Blazor with minimal configuration (for quick setup)
        /// </summary>
        public static IServiceCollection AddCheapHelpersBlazorMinimal<TUser, TContext>(
            this IServiceCollection services)
            where TUser : CheapUser
            where TContext : CheapContext
        {
            return services.AddCheapHelpersBlazor<CheapUser, CheapContext>(options =>
            {
                options.EnableLocalization = false;
                options.EnableFileDownload = false;
            });
        }
    }
}