using CheapHelpers.Blazor.Services;
using CheapHelpers.Models.Entities;
using CheapHelpers.Services.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.Blazor.Extensions;

public static class ExternalAuthBlazorExtensions
{
    /// <summary>
    /// Registers the external user provisioning bridge. When registered, external auth providers
    /// (Plex, Google, etc.) will automatically create or link CheapUser records on login.
    /// </summary>
    /// <typeparam name="TUser">Concrete CheapUser type used by the application</typeparam>
    public static IServiceCollection AddExternalUserProvisioning<TUser>(this IServiceCollection services)
        where TUser : CheapUser, new()
    {
        services.AddSingleton<Func<CheapUser>>(() => new TUser());
        services.AddScoped<IExternalUserProvisioner, ExternalUserProvisioner>();
        return services;
    }
}
