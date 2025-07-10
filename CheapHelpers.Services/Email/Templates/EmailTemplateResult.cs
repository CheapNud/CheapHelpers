namespace CheapHelpers.Services.Email.Templates
{
    /// <summary>
    /// Result of template rendering
    /// </summary>
    public class EmailTemplateResult
    {
        public string Subject { get; set; }
        public string HtmlBody { get; set; }
        public string TextBody { get; set; }
        public string FromEmail { get; set; }
        public string FromName { get; set; }
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
    }
}