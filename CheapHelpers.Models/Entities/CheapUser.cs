// CheapHelpers.Models/Entities/CheapUser.cs (Truly generic for library consumers)
using CheapHelpers;
using CheapHelpers.Extensions;
using CheapHelpers.Models.Enums;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace CheapHelpers.Models.Entities
{
    /// <summary>
    /// Base user class with common properties for CheapHelpers applications
    /// Extend this in your application for additional properties
    /// </summary>
    public abstract class CheapUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool IsDarkMode { get; set; } = false;

        // Only truly generic navigation state - no assumptions about specific sections
        /// <summary>
        /// Generic navigation state storage. Use GetExpandState/SetExpandState methods to interact with this.
        /// Library consumers can store any navigation states they need.
        /// </summary>
        public string? NavigationStateJson { get; set; } = Constants.Authentication.EmptyJsonObject;

        // Computed properties
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string Initials => $"{FirstName.FirstOrDefault()}{LastName.FirstOrDefault()}".ToUpper();

        // User preferences
        public string? PreferredLanguage { get; set; } = "en-US";
        public DateTime? LastLoginDate { get; set; }
        public bool IsFirstLogin { get; set; } = true;
        public string? TimeZoneInfoId { get; set; } = null;

        [NotMapped]
        public TimeZoneInfo TimeZoneInfo => TimeZoneInfoId != null ?
            System.TimeZoneInfo.FindSystemTimeZoneById(TimeZoneInfoId) :
            TimeZoneInfo.Local;

        /// <summary>
        /// Save the hash of the pin code for this user, used to verify the pin code when needed (only factor for now)
        /// </summary>
        public string? PinCodeHash { get; set; }

        /// <summary>
        /// Navigation property for user notification preferences
        /// </summary>
        public ICollection<UserNotificationPreference> NotificationPreferences { get; set; } = [];

        // Generic navigation state management
        private Dictionary<string, object>? _navigationStateCache;

        [NotMapped]
        private Dictionary<string, object> NavigationState
        {
            get
            {
                if (_navigationStateCache == null)
                {
                    if (string.IsNullOrEmpty(NavigationStateJson))
                    {
                        _navigationStateCache = new Dictionary<string, object>();
                    }
                    else
                    {
                        try
                        {
                            _navigationStateCache = NavigationStateJson.FromJson<Dictionary<string, object>>()
                                ?? new Dictionary<string, object>();
                        }
                        catch
                        {
                            _navigationStateCache = new Dictionary<string, object>();
                        }
                    }
                }
                return _navigationStateCache;
            }
        }

        /// <summary>
        /// Gets the expansion state for any navigation section
        /// Library consumers can use any keys they want
        /// </summary>
        /// <param name="key">Navigation section key (e.g., "Admin", "Reports", "Settings", etc.)</param>
        /// <returns>True if expanded, false if collapsed</returns>
        public bool GetExpandState(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;

            if (NavigationState.TryGetValue($"{Constants.Authentication.ExpandPrefix}{key}", out var value))
            {
                return value switch
                {
                    bool boolValue => boolValue,
                    JsonElement jsonElement when jsonElement.ValueKind == JsonValueKind.True => true,
                    JsonElement jsonElement when jsonElement.ValueKind == JsonValueKind.False => false,
                    string strValue => bool.TryParse(strValue, out var parsed) && parsed,
                    _ => false
                };
            }

            return false; // Default to collapsed
        }

        /// <summary>
        /// Sets the expansion state for any navigation section
        /// Library consumers can use any keys they want
        /// </summary>
        /// <param name="key">Navigation section key</param>
        /// <param name="expanded">True to expand, false to collapse</param>
        public void SetExpandState(string key, bool expanded)
        {
            if (string.IsNullOrWhiteSpace(key)) return;

            NavigationState[$"{Constants.Authentication.ExpandPrefix}{key}"] = expanded;

            // Update JSON representation
            try
            {
                NavigationStateJson = NavigationState.ToJson();
            }
            catch
            {
                // If serialization fails, clear the problematic state
                NavigationState.Clear();
                NavigationStateJson = Constants.Authentication.EmptyJsonObject;
            }
        }

        /// <summary>
        /// Gets all navigation states as a dictionary
        /// </summary>
        /// <returns>Dictionary of all navigation states</returns>
        public Dictionary<string, bool> GetAllExpandStates()
        {
            var states = new Dictionary<string, bool>();

            foreach (var kvp in NavigationState)
            {
                if (kvp.Key.StartsWith(Constants.Authentication.ExpandPrefix) && kvp.Value is bool boolValue)
                {
                    var key = kvp.Key[Constants.Authentication.ExpandPrefix.Length..];
                    states[key] = boolValue;
                }
            }

            return states;
        }

        /// <summary>
        /// Clears all navigation states
        /// </summary>
        public void ClearNavigationState()
        {
            _navigationStateCache?.Clear();
            NavigationStateJson = Constants.Authentication.EmptyJsonObject;
        }

        // Notification preference helper methods

        /// <summary>
        /// Gets the notification preference for a specific notification type
        /// </summary>
        /// <param name="notificationType">The type of notification (e.g., "OrderConfirmation", "SystemAlert")</param>
        /// <returns>The user's preference for this notification type, or null if not set</returns>
        public UserNotificationPreference? GetNotificationPreference(string notificationType)
        {
            if (string.IsNullOrWhiteSpace(notificationType))
                return null;

            return NotificationPreferences?.FirstOrDefault(p => p.NotificationType.Equals(notificationType, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Checks if a specific notification channel is enabled for a notification type
        /// </summary>
        /// <param name="notificationType">The type of notification</param>
        /// <param name="channel">The channel to check (InApp, Email, SMS, Push)</param>
        /// <returns>True if the channel is enabled, false otherwise. Defaults to InApp if no preference is set.</returns>
        public bool IsChannelEnabled(string notificationType, NotificationChannelFlags channel)
        {
            var preference = GetNotificationPreference(notificationType);

            if (preference == null)
            {
                // Default behavior: only InApp is enabled if no preference is set
                return channel == NotificationChannelFlags.InApp;
            }

            return preference.IsChannelEnabled(channel);
        }

        /// <summary>
        /// Gets all enabled channels for a specific notification type
        /// </summary>
        /// <param name="notificationType">The type of notification</param>
        /// <returns>Flags representing all enabled channels. Defaults to InApp if no preference is set.</returns>
        public NotificationChannelFlags GetEnabledChannels(string notificationType)
        {
            var preference = GetNotificationPreference(notificationType);
            return preference?.EnabledChannels ?? NotificationChannelFlags.InApp;
        }
    }
}

// Example: How library consumers would extend it in their applications
/*
// YourApp/Models/ApplicationUser.cs
using CheapHelpers.Models.Entities;

namespace YourApp.Models
{
    public class ApplicationUser : CheapUser
    {
        // Add any app-specific properties here
        public string? Department { get; set; }
        
        // If you want strongly-typed navigation properties (optional), add them:
        public bool IsAdminNavExpanded
        {
            get => GetExpandState("Admin");
            set => SetExpandState("Admin", value);
        }
        
        public bool IsReportsNavExpanded
        {
            get => GetExpandState("Reports");
            set => SetExpandState("Reports", value);
        }
        
        // Or just use the generic methods directly:
        // user.GetExpandState("Admin")
        // user.SetExpandState("Admin", true)
    }
}
*/