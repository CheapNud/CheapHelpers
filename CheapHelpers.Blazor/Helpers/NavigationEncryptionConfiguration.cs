namespace CheapHelpers.Blazor.Helpers
{
    /// <summary>
    /// Configuration for role-based parameter encryption
    /// </summary>
    public class NavigationEncryptionConfiguration
    {
        /// <summary>
        /// Dictionary mapping roles to the specific parameters that need to be encrypted for that role
        /// </summary>
        public Dictionary<string, List<string>> RoleBasedEncryptionParams { get; set; } = new();

        /// <summary>
        /// Cache duration for encryption parameters in minutes (default: 5 minutes)
        /// </summary>
        public int CacheDurationMinutes { get; set; } = 5;

        /// <summary>
        /// Add a role with its encryption parameters
        /// </summary>
        /// <param name="role">Role name</param>
        /// <param name="parametersToEncrypt">Parameters to encrypt for this role</param>
        /// <returns>Configuration instance for fluent API</returns>
        public NavigationEncryptionConfiguration AddRole(string role, params string[] parametersToEncrypt)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(role);
            ArgumentNullException.ThrowIfNull(parametersToEncrypt);

            RoleBasedEncryptionParams[role] = [.. parametersToEncrypt];
            return this;
        }

        /// <summary>
        /// Add a role with its encryption parameters using collection
        /// </summary>
        /// <param name="role">Role name</param>
        /// <param name="parametersToEncrypt">Parameters to encrypt for this role</param>
        /// <returns>Configuration instance for fluent API</returns>
        public NavigationEncryptionConfiguration AddRole(string role, IEnumerable<string> parametersToEncrypt)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(role);
            ArgumentNullException.ThrowIfNull(parametersToEncrypt);

            RoleBasedEncryptionParams[role] = parametersToEncrypt.ToList();
            return this;
        }

        /// <summary>
        /// Remove a role configuration
        /// </summary>
        /// <param name="role">Role name to remove</param>
        /// <returns>Configuration instance for fluent API</returns>
        public NavigationEncryptionConfiguration RemoveRole(string role)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(role);
            RoleBasedEncryptionParams.Remove(role);
            return this;
        }

        /// <summary>
        /// Clear all role configurations
        /// </summary>
        /// <returns>Configuration instance for fluent API</returns>
        public NavigationEncryptionConfiguration Clear()
        {
            RoleBasedEncryptionParams.Clear();
            return this;
        }

        /// <summary>
        /// Set cache duration
        /// </summary>
        /// <param name="minutes">Cache duration in minutes</param>
        /// <returns>Configuration instance for fluent API</returns>
        public NavigationEncryptionConfiguration SetCacheDuration(int minutes)
        {
            if (minutes < 0)
                throw new ArgumentOutOfRangeException(nameof(minutes), "Cache duration must be non-negative");

            CacheDurationMinutes = minutes;
            return this;
        }
    }
}