using System.Diagnostics;
using CheapHelpers.Models.Entities;
using CheapHelpers.Services.Auth;
using Microsoft.AspNetCore.Identity;

namespace CheapHelpers.Blazor.Services;

/// <summary>
/// Default implementation that provisions CheapUser records from external auth providers.
/// Requires a <see cref="Func{CheapUser}"/> factory to be registered in DI for creating concrete user instances.
/// </summary>
public class ExternalUserProvisioner(
    UserManager<CheapUser> userManager,
    Func<CheapUser> userFactory) : IExternalUserProvisioner
{
    public async Task<ExternalProvisionResult> FindOrCreateUserAsync(ExternalUserInfo userInfo, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userInfo);

        try
        {
            // 1. Check if external login already linked
            var existingUser = await userManager.FindByLoginAsync(userInfo.ProviderName, userInfo.ExternalId);
            if (existingUser is not null)
            {
                Debug.WriteLine($"External user {userInfo.ProviderName}:{userInfo.ExternalId} found by login link: {existingUser.Id}");
                return ExternalProvisionResult.Succeeded(existingUser.Id, existingUser.UserName ?? existingUser.Email ?? userInfo.Username ?? "", isNew: false);
            }

            // 2. Try to find by email and link
            if (!string.IsNullOrEmpty(userInfo.Email))
            {
                var emailUser = await userManager.FindByEmailAsync(userInfo.Email);
                if (emailUser is not null)
                {
                    var linkResult = await userManager.AddLoginAsync(emailUser, new UserLoginInfo(userInfo.ProviderName, userInfo.ExternalId, userInfo.ProviderName));
                    if (linkResult.Succeeded)
                    {
                        Debug.WriteLine($"Linked {userInfo.ProviderName}:{userInfo.ExternalId} to existing user {emailUser.Id} by email");
                        return ExternalProvisionResult.Succeeded(emailUser.Id, emailUser.UserName ?? emailUser.Email ?? "", isNew: false);
                    }

                    Debug.WriteLine($"Failed to link external login to existing user: {string.Join(", ", linkResult.Errors.Select(e => e.Description))}");
                }
            }

            // 3. Create new user
            var newUser = userFactory();
            newUser.UserName = userInfo.Email ?? $"{userInfo.ProviderName}_{userInfo.ExternalId}";
            newUser.Email = userInfo.Email;
            newUser.EmailConfirmed = userInfo.Email is not null; // Trust the provider's email
            newUser.FirstName = userInfo.Username ?? "";
            newUser.IsFirstLogin = true;

            var createResult = await userManager.CreateAsync(newUser);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                Debug.WriteLine($"Failed to create user from {userInfo.ProviderName}: {errors}");
                return ExternalProvisionResult.Failed(errors);
            }

            var addLoginResult = await userManager.AddLoginAsync(newUser, new UserLoginInfo(userInfo.ProviderName, userInfo.ExternalId, userInfo.ProviderName));
            if (!addLoginResult.Succeeded)
            {
                Debug.WriteLine($"User created but failed to add external login: {string.Join(", ", addLoginResult.Errors.Select(e => e.Description))}");
            }

            Debug.WriteLine($"Created new user {newUser.Id} from {userInfo.ProviderName}:{userInfo.ExternalId}");
            return ExternalProvisionResult.Succeeded(newUser.Id, newUser.UserName, isNew: true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"External user provisioning failed: {ex.Message}");
            return ExternalProvisionResult.Failed(ex.Message);
        }
    }
}
