using CheapHelpers.Services.Pdf.Export;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Event;
using System.Diagnostics;
using iTextRectangle = iText.Kernel.Geom.Rectangle; // Resolve ambiguity

namespace CheapHelpers.Services.Pdf.Templates
{
    // Enhanced event handler for templated headers/footers  
    public class TemplatedHeaderFooterHandler : AbstractPdfDocumentEventHandler
    {
        private readonly string? _headerTemplate;
        private readonly string? _footerTemplate;
        private readonly PdfColorScheme _colorScheme;

        public TemplatedHeaderFooterHandler(string? headerTemplate, string? footerTemplate, PdfColorScheme colorScheme)
        {
            _headerTemplate = headerTemplate;
            _footerTemplate = footerTemplate;
            _colorScheme = colorScheme;
        }

        protected override void OnAcceptedEvent(AbstractPdfDocumentEvent currentEvent)
        {
            if (currentEvent is PdfDocumentEvent docEvent)
            {
                var pdfDoc = docEvent.GetDocument();
                var page = docEvent.GetPage();
                var pageSize = page.GetPageSize();
                var canvas = new PdfCanvas(page.NewContentStreamBefore(), page.GetResources(), pdfDoc);

                try
                {
                    var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                    // Draw footer
                    if (!string.IsNullOrWhiteSpace(_footerTemplate))
                    {
                        DrawFooter(canvas, pageSize, font);
                    }

                    // Draw header 
                    if (!string.IsNullOrWhiteSpace(_headerTemplate))
                    {
                        DrawHeader(canvas, pageSize, font);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in template handler: {ex.Message}");
                }
            }
        }

        private void DrawFooter(PdfCanvas canvas, iTextRectangle pageSize, PdfFont font)
        {
            const float FOOTER_HEIGHT = 30f;
            const float MARGIN = 20f;

            canvas.SaveState()
                .SetStrokeColor(_colorScheme.Border)
                .SetLineWidth(0.5f)
                .MoveTo(pageSize.GetLeft() + MARGIN, pageSize.GetBottom() + FOOTER_HEIGHT)
                .LineTo(pageSize.GetRight() - MARGIN, pageSize.GetBottom() + FOOTER_HEIGHT)
                .Stroke();

            var footerText = _footerTemplate ?? "____________________________________________";
            var textWidth = font.GetWidth(footerText, 7);
            var xPosition = (pageSize.GetLeft() + pageSize.GetRight() - textWidth) / 2;

            canvas.BeginText()
                .SetFontAndSize(font, 7)
                .SetColor(_colorScheme.Secondary, true)
                .MoveText(xPosition, pageSize.GetBottom() + 15)
                .ShowText(footerText)
                .EndText()
                .RestoreState();
        }

        private void DrawHeader(PdfCanvas canvas, iTextRectangle pageSize, PdfFont font)
        {
            const float HEADER_HEIGHT = 50f;

            var headerText = _headerTemplate ?? "";
            if (!string.IsNullOrWhiteSpace(headerText))
            {
                var textWidth = font.GetWidth(headerText, 10);
                var xPosition = (pageSize.GetLeft() + pageSize.GetRight() - textWidth) / 2;

                canvas.BeginText()
                    .SetFontAndSize(font, 10)
                    .SetColor(_colorScheme.Primary, true)
                    .MoveText(xPosition, pageSize.GetTop() - HEADER_HEIGHT)
                    .ShowText(headerText)
                    .EndText();
            }
        }
    }
}