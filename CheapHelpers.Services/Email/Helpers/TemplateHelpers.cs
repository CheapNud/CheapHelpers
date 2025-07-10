namespace CheapHelpers.Services.Email.Helpers
{
    /// <summary>
    /// Helper class providing utility values and flags for templates
    /// </summary>
    public class TemplateHelpers
    {
        public bool IsTestEnvironment { get; set; }
        public int CurrentYear { get; set; } = DateTime.Now.Year;
        public string BrandName { get; set; } = "Default Brand";
        public string DefaultEmailFrom { get; set; } = "noreply@example.com";
        public string MachineName { get; set; } = Environment.MachineName;
        public bool DisplayHelp { get; set; }
        public bool DisplayMoreInformation { get; set; }
        public bool OverrideDisplayHelp { get; set; }
    }
}