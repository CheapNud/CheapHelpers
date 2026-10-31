namespace CheapHelpers.Models.Contracts
{
    /// <summary>
    /// Base interface for all email template data
    /// </summary>
    public interface IEmailTemplateData
    {
        string Subject { get; }
        string Recipient { get; set; }
        string TraceId { get; set; }
    }
}