using CheapHelpers.Services.Billing.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.Services.Billing.Extensions;

/// <summary>
/// Extension methods for registering CheapHelpers billing services with dependency injection.
/// </summary>
public static class BillingServiceExtensions
{
    /// <summary>
    /// Registers the CheapHelpers billing and usage metering services.
    /// </summary>
    /// <typeparam name="TUser">The user type that inherits from IdentityUser.</typeparam>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureOptions">Optional action to configure billing options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCheapBilling<TUser>(
        this IServiceCollection services,
        Action<BillingOptions>? configureOptions = null)
        where TUser : IdentityUser
    {
        var billingOpts = new BillingOptions();
        configureOptions?.Invoke(billingOpts);

        services.AddSingleton(billingOpts);
        services.AddScoped<IUsageMeterService, UsageMeterService<TUser>>();
        services.AddScoped<IBillingService, BillingService<TUser>>();

        return services;
    }
}
