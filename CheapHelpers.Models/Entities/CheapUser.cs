using Microsoft.AspNetCore.Identity;

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

        // Navigation expansion states
        public bool IsMainNavExpanded { get; set; } = true;
        public bool IsAccountNavExpanded { get; set; } = false;
        public bool IsAdminNavExpanded { get; set; } = false;

        // Computed properties
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string Initials => $"{FirstName.FirstOrDefault()}{LastName.FirstOrDefault()}".ToUpper();

        // User preferences
        public string? PreferredLanguage { get; set; } = "en-US";
        public DateTime? LastLoginDate { get; set; }
        public bool IsFirstLogin { get; set; } = true;
    }
}