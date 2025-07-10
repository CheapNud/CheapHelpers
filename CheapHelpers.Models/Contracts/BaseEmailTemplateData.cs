namespace CheapHelpers.Models.Contracts
{
    /// <summary>
    /// Base implementation for email template data
    /// </summary>
    public abstract class BaseEmailTemplateData : IEmailTemplateData
    {
        public abstract string Subject { get; }
        public string Recipient { get; set; }
        public string TraceId { get; set; }
    }
}