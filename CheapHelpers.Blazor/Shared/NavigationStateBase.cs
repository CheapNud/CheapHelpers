using CheapHelpers.Blazor.Services;
using CheapHelpers.Models.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Diagnostics;

namespace CheapHelpers.Blazor.Shared
{


    // CheapHelpers.Blazor/Components/NavigationStateBase.cs (This stays)

    /// <summary>
    /// Base component for navigation components (not layouts)
    /// Use this for NavMenu.razor and other navigation components
    /// </summary>
    public abstract class NavigationStateBase<TUser> : ComponentBase, IDisposable where TUser : CheapUser
    {
        protected CheapUser? User { get; set; }
        protected Dictionary<string, bool> LocalNavigationState { get; set; } = new();
        protected bool IsInitialized { get; set; } = false;

        [CascadingParameter] protected Task<AuthenticationState> AuthenticationState { get; set; } = default!;
        [Inject] protected IAccountIdentityService AccountIdentity { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                // Null when signed out
                User = await AccountIdentity.GetUserAsync((await AuthenticationState).User);
                if (User != null)
                {
                    var allStates = User.GetAllExpandStates();
                    foreach (var kvp in allStates)
                    {
                        LocalNavigationState[kvp.Key] = kvp.Value;
                    }
                    Debug.WriteLine($"NavigationStateBase: Loaded {LocalNavigationState.Count} navigation states for user {User.UserName}");
                }
                IsInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing NavigationStateBase: {ex.Message}");
            }
        }

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
                Debug.WriteLine($"NavigationStateBase: Navigation state updated: {key} = {expanded}");

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await AccountIdentity.UpdateNavigationStatesAsync(User.Id, new() { [key] = expanded });
                        Debug.WriteLine($"NavigationStateBase: Navigation state saved to database: {key} = {expanded}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error saving navigation state to database: {ex.Message}");
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
            if (!IsInitialized || User == null || LocalNavigationState.Count == 0) return;

            try
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await AccountIdentity.UpdateNavigationStatesAsync(User.Id, LocalNavigationState);
                        Debug.WriteLine($"NavigationStateBase: Batch saved {LocalNavigationState.Count} navigation states on dispose");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error batch saving navigation states: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in NavigationStateBase dispose: {ex.Message}");
            }
        }
    }
}