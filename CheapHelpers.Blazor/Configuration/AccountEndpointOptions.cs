using CheapHelpers.Models.Entities;
using Microsoft.AspNetCore.Http;

namespace CheapHelpers.Blazor.Configuration;

/// <summary>
/// Options for <see cref="Extensions.AccountEndpointExtensions.MapCheapAccountEndpoints{TUser}"/>.
/// </summary>
public sealed class AccountEndpointOptions<TUser> where TUser : CheapUser
{
    /// <summary>
    /// Runs after a successful password, 2FA or recovery-code sign-in.
    /// Return a redirect path to override <see cref="Pages.Account.AccountRouteOptions.HomeRoute"/>, or null to keep it.
    /// </summary>
    public Func<TUser, HttpContext, Task<string?>>? AfterSignIn { get; set; }

    /// <summary>
    /// Runs after the change-password endpoint changed the password and refreshed the cookie.
    /// Return a redirect path to override <see cref="Pages.Account.AccountRouteOptions.HomeRoute"/>, or null to keep it.
    /// </summary>
    public Func<TUser, HttpContext, Task<string?>>? AfterPasswordChanged { get; set; }
}
