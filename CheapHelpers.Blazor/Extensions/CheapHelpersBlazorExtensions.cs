using CheapHelpers.Blazor.Abstractions;
using CheapHelpers.Blazor.Configuration;
using CheapHelpers.Blazor.Services;
using CheapHelpers.Services.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace CheapHelpers.Blazor.Extensions
{
    public static class CheapHelpersBlazorExtensions
    {
        /// <summary>
        /// Adds CheapHelpers Blazor components and services to the service collection
        /// </summary>
        /// <typeparam name="TContext">The application's DbContext type</typeparam>
        /// <typeparam name="TUser">The application's user type</typeparam>
        public static IServiceCollection AddCheapHelpersBlazor<TContext, TUser>(
            this IServiceCollection services,
            Action<CheapHelpersBlazorOptions>? configure = null)
            where TContext : DbContext
            where TUser : IdentityUser, new()
        {
            var options = new CheapHelpersBlazorOptions();
            configure?.Invoke(options);

            // Add MudBlazor if not already added
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
            services.AddScoped<IUserService<TUser>, UserService<TUser>>();

            // Register email service if configured
            if (options.EmailServiceType != null)
            {
                services.AddScoped(typeof(IEmailService), options.EmailServiceType);
            }

            // Register repositories if base repo is configured
            if (options.UseRepositories)
            {
                services.AddScoped<BaseRepo<TContext>();
                services.AddScoped<UserRepo<TContext, TUser>>();
            }

            // Register additional services based on options
            if (options.EnableFileDownload)
            {
                // Add any file download service registration here
                // services.AddBlazorDownloadFile();
            }

            // Register custom services
            foreach (var customService in options.CustomServices)
            {
                services.Add(customService);
            }

            return services;
        }

        /// <summary>
        /// Adds CheapHelpers Blazor with minimal configuration
        /// </summary>
        public static IServiceCollection AddCheapHelpersBlazor(
            this IServiceCollection services)
        {
            return services.AddCheapHelpersBlazor<DbContext, IdentityUser>();
        }
    }

    /// <summary>
    /// Configuration options for CheapHelpers Blazor
    /// </summary>
    public class CheapHelpersBlazorOptions
    {
        /// <summary>
        /// Enable localization support
        /// </summary>
        public bool EnableLocalization { get; set; } = true;

        /// <summary>
        /// Enable file download functionality
        /// </summary>
        public bool EnableFileDownload { get; set; } = false;

        /// <summary>
        /// Use repository pattern
        /// </summary>
        public bool UseRepositories { get; set; } = true;

        /// <summary>
        /// Snackbar position
        /// </summary>
        public string SnackbarPosition { get; set; } = Defaults.Classes.Position.BottomLeft;

        /// <summary>
        /// Prevent duplicate snackbars
        /// </summary>
        public bool PreventDuplicateSnackbars { get; set; } = true;

        /// <summary>
        /// Maximum number of displayed snackbars
        /// </summary>
        public int MaxSnackbars { get; set; } = 3;

        /// <summary>
        /// Snackbar visible duration in milliseconds
        /// </summary>
        public int SnackbarDuration { get; set; } = 3000;

        /// <summary>
        /// Type of email service to use
        /// </summary>
        public Type? EmailServiceType { get; set; }

        /// <summary>
        /// Custom service registrations
        /// </summary>
        public List<ServiceDescriptor> CustomServices { get; set; } = new();

        /// <summary>
        /// Add a custom service registration
        /// </summary>
        public CheapHelpersBlazorOptions AddCustomService<TService, TImplementation>(
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where TService : class
            where TImplementation : class, TService
        {
            CustomServices.Add(new ServiceDescriptor(typeof(TService), typeof(TImplementation), lifetime));
            return this;
        }
    }
}