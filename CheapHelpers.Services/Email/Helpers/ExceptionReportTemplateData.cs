using CheapHelpers.Helpers.Logs;
using CheapHelpers.Models.Contracts;

namespace CheapHelpers.Services.Email.Helpers;

public class ExceptionReportTemplateData : BaseEmailTemplateData
{
    public override string Subject { get; } = "Developer info exception";

    public ExceptionReport Report { get; set; } = new();

    public string[]? AdditionalParameters { get; set; }

    public ExceptionReportTemplateData() { }

    public ExceptionReportTemplateData(string subject)
    {
        Subject = subject;
    }
}
