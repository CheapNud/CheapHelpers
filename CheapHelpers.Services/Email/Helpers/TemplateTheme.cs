namespace CheapHelpers.Services.Email.Helpers
{
    /// <summary>
    /// Extended theme info for templates with styling properties
    /// Simplified version without external dependencies
    /// </summary>
    public class TemplateTheme
    {
        public string BrandName { get; set; } = "Default Brand";
        public string EmailFrom { get; set; } = "noreply@example.com";
        public string Footer { get; set; } = "";
        public string Template { get; set; } = "";

        // Styling properties
        public string Primary { get; set; } = "#007bff";
        public string Header { get; set; } = "#ffffff";
        public string HeaderText { get; set; } = "#000000";
        public string BrandLogoSize { get; set; } = "200px";

        /// <summary>
        /// Create a default theme - customize this for your needs
        /// </summary>
        public static TemplateTheme CreateDefault(string brandName = null, string emailFrom = null)
        {
            return new TemplateTheme
            {
                BrandName = brandName ?? "Default Brand",
                EmailFrom = emailFrom ?? "noreply@example.com",
                Footer = $"© {DateTime.Now.Year} {brandName ?? "Default Brand"}. All rights reserved.",
                Primary = "#007bff",
                Header = "#ffffff",
                HeaderText = "#000000",
                BrandLogoSize = "200px"
            };
        }
    }
}