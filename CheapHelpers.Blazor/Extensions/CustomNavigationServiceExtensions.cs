using CheapHelpers.Blazor.Helpers;
using CheapHelpers.Blazor.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.Blazor.Extensions
{
    public static class CustomNavigationServiceExtensions
    {
        /// <summary>
        /// Registers the CustomNavigationService with role-based encryption configuration
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configure">Configuration action for setting up role-based encryption</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddCustomNavigationService(
            this IServiceCollection services,
            Action<NavigationEncryptionConfiguration>? configure = null)
        {
            // Create and configure the navigation encryption configuration
            var config = new NavigationEncryptionConfiguration();
            configure?.Invoke(config);

            // Register the configuration as singleton
            services.AddSingleton(config);

            // Register the CustomNavigationService as scoped
            services.AddScoped<CustomNavigationService>();

            return services;
        }

        /// <summary>
        /// Registers the CustomNavigationService with pre-built configuration
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Pre-configured NavigationEncryptionConfiguration</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddCustomNavigationService(
            this IServiceCollection services,
            NavigationEncryptionConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            // Register the configuration as singleton
            services.AddSingleton(configuration);

            // Register the CustomNavigationService as scoped
            services.AddScoped<CustomNavigationService>();

            return services;
        }

        /// <summary>
        /// Registers the CustomNavigationService with simple role-parameter mapping
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="roleParameterMappings">Dictionary mapping roles to their encrypted parameters</param>
        /// <param name="cacheDurationMinutes">Cache duration in minutes (default: 5)</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddCustomNavigationService(
            this IServiceCollection services,
            Dictionary<string, string[]> roleParameterMappings,
            int cacheDurationMinutes = 5)
        {
            ArgumentNullException.ThrowIfNull(roleParameterMappings);

            var config = new NavigationEncryptionConfiguration()
                .SetCacheDuration(cacheDurationMinutes);

            foreach (var (role, parameters) in roleParameterMappings)
            {
                config.AddRole(role, parameters);
            }

            return services.AddCustomNavigationService(config);
        }
    }
}

// USAGE EXAMPLES:

/*
// Example 1: Using fluent configuration
builder.Services.AddCustomNavigationService(config =>
{
    config.AddRole("Roles.ServiceExternal", "OrderId", "ServiceId")
          .AddRole("Roles.Admin", "UserId", "CompanyId")
          .AddRole("Roles.Manager", "OrderId")
          .SetCacheDuration(10); // 10 minutes cache
});

// Example 2: Using dictionary mapping
var roleMappings = new Dictionary<string, string[]>
{
    { "Roles.ServiceExternal", ["OrderId", "ServiceId"] },
    { "Roles.Admin", ["UserId", "CompanyId"] },
    { "Roles.Manager", ["OrderId"] }
};
builder.Services.AddCustomNavigationService(roleMappings, cacheDurationMinutes: 15);

// Example 3: Using pre-built configuration
var config = new NavigationEncryptionConfiguration()
    .AddRole("Roles.ServiceExternal", "OrderId", "ServiceId")
    .AddRole("Roles.Admin", "UserId", "CompanyId")
    .SetCacheDuration(5);
    
builder.Services.AddCustomNavigationService(config);

// Example 4: Add to existing CheapHelpersBlazor setup
builder.Services.AddCheapHelpersBlazor<YourUserType>()
                .AddCustomNavigationService(config =>
                {
                    config.AddRole("Roles.ServiceExternal", "OrderId")
                          .AddRole("ServiceSupplier", "serviceId", "orderId");
                });

// Example 5: In your existing CheapHelpersBlazorExtensions.cs
public static IServiceCollection AddCheapHelpersBlazor<TUser>(
    this IServiceCollection services,
    Action<CheapHelpersBlazorOptions>? configure = null)
    where TUser : CheapUser
{
    // ... existing configuration ...

    // Add navigation service with default configuration
    services.AddCustomNavigationService(config =>
    {
        config.AddRole("Roles.ServiceExternal", "OrderId");
        // Add more roles as needed
    });

    return services;
}
*/