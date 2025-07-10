using CheapHelpers.Services.DataExchange.Pdf.Helpers;
using iText.Commons.Actions;
using iText.Kernel.Pdf;
using iText.Layout;

namespace CheapHelpers.Services.DataExchange.Pdf
{
    // Template service interfaces
    public interface IPdfTemplateService
    {
        void AddHeader(Document document, string? template, PdfColorScheme colorScheme);
        void AddFooter(PdfDocument pdfDocument, string? template, PdfColorScheme colorScheme);
        IEventHandler CreateHeaderFooterHandler(string? headerTemplate, string? footerTemplate, PdfColorScheme colorScheme);
    }
}