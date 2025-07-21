// CheapHelpers.Blazor/Components/UserPreferencesBase.cs
using CheapHelpers.Blazor.Services;
using CheapHelpers.Models.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Diagnostics;

namespace CheapHelpers.Blazor.Shared
{
    /// <summary>
    /// Base component that provides user preference management (dark mode, etc.)
    /// Library consumers can inherit from this to get user preference features
    /// </summary>
    public abstract class UserPreferencesBase<TUser> : LayoutComponentBase where TUser : CheapUser
    {
        protected CheapUser? User { get; set; }
        protected bool DarkMode { get; set; } = false;
        protected bool IsInitialized { get; set; } = false;

        [CascadingParameter] protected Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;
        [Inject] protected UserService UserService { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            await LoadUserPreferencesAsync();
        }

        /// <summary>
        /// Loads user preferences from the database
        /// </summary>
        protected async Task LoadUserPreferencesAsync()
        {
            try
            {
                if (await UserService.IsAuthenticated(AuthenticationStateTask))
                {
                    User = await UserService.GetUserAsync(AuthenticationStateTask);
                    if (User != null)
                    {
                        DarkMode = User.IsDarkMode;
                        Debug.WriteLine($"UserPreferencesBase: Loaded preferences for user {User.UserName} - DarkMode: {DarkMode}");
                    }
                }
                IsInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading user preferences: {ex.Message}");
                IsInitialized = true; // Still mark as initialized to prevent blocking
            }
        }

        /// <summary>
        /// Toggles dark mode and saves to database
        /// </summary>
        protected async Task ToggleDarkModeAsync()
        {
            if (User == null) return;

            try
            {
                DarkMode = !DarkMode;
                User.IsDarkMode = DarkMode;

                Debug.WriteLine($"UserPreferencesBase: Toggling dark mode to {DarkMode} for user {User.UserName}");

                // Save to database asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await UserService.UpdateUserAsync(User);
                        Debug.WriteLine($"UserPreferencesBase: Dark mode preference saved");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error saving dark mode preference: {ex.Message}");
                    }
                });

                StateHasChanged(); // Update UI immediately
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error toggling dark mode: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets dark mode state and saves to database
        /// </summary>
        protected async Task SetDarkModeAsync(bool darkMode)
        {
            if (User == null || DarkMode == darkMode) return;

            try
            {
                DarkMode = darkMode;
                User.IsDarkMode = darkMode;

                Debug.WriteLine($"UserPreferencesBase: Setting dark mode to {darkMode} for user {User.UserName}");

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await UserService.UpdateUserAsync(User);
                        Debug.WriteLine($"UserPreferencesBase: Dark mode preference saved");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error saving dark mode preference: {ex.Message}");
                    }
                });

                StateHasChanged();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting dark mode: {ex.Message}");
            }
        }
    }
}