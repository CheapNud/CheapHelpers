using CheapHelpers.Blazor.Services;
using CheapHelpers.Models.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Diagnostics;

namespace CheapHelpers.Blazor.Shared
{
    /// <summary>
    /// Base class for layout components that need user preferences and navigation state
    /// Inherits from LayoutComponentBase to provide the Body property
    /// </summary>
    public abstract class LayoutBase<TUser> : LayoutComponentBase, IDisposable where TUser : CheapUser
    {
        protected CheapUser? User { get; set; }
        protected bool DarkMode { get; set; } = false;
        protected bool IsInitialized { get; set; } = false;

        // Navigation state properties (if the layout needs them)
        protected Dictionary<string, bool> LocalNavigationState { get; set; } = new();

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

                        // Load navigation states if needed
                        var allStates = User.GetAllExpandStates();
                        foreach (var kvp in allStates)
                        {
                            LocalNavigationState[kvp.Key] = kvp.Value;
                        }

                        Debug.WriteLine($"LayoutBase: Loaded preferences for user {User.UserName} - DarkMode: {DarkMode}");
                    }
                }
                IsInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading user preferences in LayoutBase: {ex.Message}");
                IsInitialized = true;
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

                Debug.WriteLine($"LayoutBase: Toggling dark mode to {DarkMode} for user {User.UserName}");

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await UserService.UpdateUserAsync(User);
                        Debug.WriteLine($"LayoutBase: Dark mode preference saved");
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
                Debug.WriteLine($"Error toggling dark mode: {ex.Message}");
            }
        }

        /// <summary>
        /// Navigation state methods (if layout needs to manage nav state)
        /// </summary>
        protected bool GetExpandState(string key)
        {
            if (!IsInitialized || User == null) return false;

            if (LocalNavigationState.TryGetValue(key, out var cachedValue))
                return cachedValue;

            var userValue = User.GetExpandState(key);
            LocalNavigationState[key] = userValue;
            return userValue;
        }

        protected void SaveNavigationState(string key, bool expanded)
        {
            if (!IsInitialized || User == null) return;

            try
            {
                LocalNavigationState[key] = expanded;
                User.SetExpandState(key, expanded);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await UserService.UpdateNavigationStateAsync(User.Id, key, expanded);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error saving navigation state: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating navigation state: {ex.Message}");
            }
        }

        public virtual void Dispose()
        {
            // Batch save navigation states on dispose
            if (IsInitialized && User != null && LocalNavigationState.Count > 0)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await UserService.UpdateNavigationStatesAsync(User.Id, LocalNavigationState);
                        Debug.WriteLine($"LayoutBase: Batch saved {LocalNavigationState.Count} navigation states on dispose");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error batch saving navigation states: {ex.Message}");
                    }
                });
            }
        }
    }
}