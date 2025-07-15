using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace CheapHelpers.Blazor.Configuration
{
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
        /// Enable two-factor authentication features
        /// </summary>
        public bool EnableTwoFactor { get; set; } = false;

        /// <summary>
        /// Enable GDPR features (personal data download, etc.)
        /// </summary>
        public bool EnableGdprFeatures { get; set; } = true;

        /// <summary>
        /// Snackbar position for MudBlazor notifications
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
        /// Type of email service implementation to register
        /// </summary>
        public Type? EmailServiceType { get; set; }

        /// <summary>
        /// Default culture for localization
        /// </summary>
        public string DefaultCulture { get; set; } = "en-US";

        /// <summary>
        /// Supported cultures for the application
        /// </summary>
        public string[] SupportedCultures { get; set; } = ["en-US"];

        /// <summary>
        /// Custom CSS classes for MudBlazor theme overrides
        /// </summary>
        public string? CustomThemeClass { get; set; }

        /// <summary>
        /// Custom service registrations
        /// </summary>
        public List<ServiceDescriptor> CustomServices { get; set; } = [];

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

        /// <summary>
        /// Set the email service implementation
        /// </summary>
        public CheapHelpersBlazorOptions UseEmailService<TEmailService>()
            where TEmailService : class
        {
            EmailServiceType = typeof(TEmailService);
            return this;
        }
    }
}
