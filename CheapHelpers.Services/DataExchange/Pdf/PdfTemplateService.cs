using CheapHelpers.Services.DataExchange.Pdf.Helpers;
using CheapHelpers.Services.DataExchange.Pdf.Templates;
using iText.Commons.Actions;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Event;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using iTextParagraph = iText.Layout.Element.Paragraph; // Resolve ambiguity
using iTextTable = iText.Layout.Element.Table; // Resolve ambiguity

namespace CheapHelpers.Services.DataExchange.Pdf
{
    // Template service implementation
    public class PdfTemplateService : IPdfTemplateService
    {
        private const string COMPANY_NAME = "COMPANY_NAME";
        private const string COMPANY_ADDRESS = "COMPANY_ADDRESS";
        private const string COMPANY_VAT = "COMPANY_VAT";

        public void AddHeader(Document document, string? template, PdfColorScheme colorScheme)
        {
            var headerTable = new iTextTable([2f, 1f]).UseAllAvailableWidth();

            var companyCell = new Cell()
                .Add(new iTextParagraph(COMPANY_NAME).SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD)))
                .Add(new iTextParagraph(COMPANY_ADDRESS).SetFontSize(10))
                .Add(new iTextParagraph(COMPANY_VAT).SetFontSize(10))
                .SetBorder(Border.NO_BORDER);

            headerTable.AddCell(companyCell);

            var rightCell = new Cell()
                .Add(new iTextParagraph(template ?? DateTime.Now.ToString("dd/MM/yyyy")).SetFontSize(10))
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetBorder(Border.NO_BORDER);

            headerTable.AddCell(rightCell);
            document.Add(headerTable.SetMarginBottom(10));
        }

        public void AddFooter(PdfDocument pdfDocument, string? template, PdfColorScheme colorScheme)
        {
            var handler = CreateHeaderFooterHandler(null, template, colorScheme);
            pdfDocument.AddEventHandler(PdfDocumentEvent.END_PAGE, handler as AbstractPdfDocumentEventHandler);
        }

        public IEventHandler CreateHeaderFooterHandler(string? headerTemplate, string? footerTemplate, PdfColorScheme colorScheme)
        {
            return new TemplatedHeaderFooterHandler(headerTemplate, footerTemplate, colorScheme);
        }
    }
}
