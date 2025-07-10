using CheapHelpers.Services.Pdf.Configuration;
using CheapHelpers.Services.Pdf.Templates;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Event;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using System.Reflection;
using iTextParagraph = iText.Layout.Element.Paragraph; // Resolve ambiguity
using iTextTable = iText.Layout.Element.Table; // Resolve ambiguity

namespace CheapHelpers.Services.Pdf.Export
{
    // Generic export service implementation
    public class PdfExportService : IPdfExportService
    {
        private readonly IPdfTemplateService _templateService;

        public PdfExportService(IPdfTemplateService templateService)
        {
            _templateService = templateService;
        }

        public async Task<byte[]> ExportToPdfAsync<T>(IEnumerable<T> data, PdfDocumentTemplate template)
        {
            using var memoryStream = new MemoryStream();
            await GeneratePdfAsync(data, template, memoryStream);
            return memoryStream.ToArray();
        }

        public async Task ExportToPdfFileAsync<T>(IEnumerable<T> data, PdfDocumentTemplate template, string filePath)
        {
            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            await GeneratePdfAsync(data, template, fileStream);
        }

        public async Task<byte[]> ExportSingleToPdfAsync<T>(T entity, PdfDocumentTemplate template)
        {
            return await ExportToPdfAsync([entity], template);
        }

        private async Task GeneratePdfAsync<T>(IEnumerable<T> data, PdfDocumentTemplate template, Stream stream)
        {
            await Task.Run(() =>
            {
                using var writer = new PdfWriter(stream);
                using var pdfDocument = new PdfDocument(writer);
                pdfDocument.SetDefaultPageSize(template.PageSize);

                var document = new Document(pdfDocument);
                var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                document.SetFont(font);

                if (template.UseHeader || template.UseFooter)
                {
                    var handler = _templateService.CreateHeaderFooterHandler(
                        template.UseHeader ? template.HeaderTemplate : null,
                        template.UseFooter ? template.FooterTemplate : null,
                        template.ColorScheme);
                    pdfDocument.AddEventHandler(PdfDocumentEvent.END_PAGE, handler as AbstractPdfDocumentEventHandler);
                }

                if (!string.IsNullOrWhiteSpace(template.Title))
                {
                    document.Add(new iTextParagraph(template.Title)
                        .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                        .SetFontSize(16)
                        .SetFontColor(template.ColorScheme.Primary)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginBottom(10));
                }

                if (template.Sections.Count > 0)
                {
                    GenerateSectionedContent(document, data, template);
                }
                else
                {
                    GenerateTableContent(document, data, template);
                }

                document.Close();
            });
        }

        private void GenerateSectionedContent<T>(Document document, IEnumerable<T> data, PdfDocumentTemplate template)
        {
            foreach (var section in template.Sections)
            {
                var sectionParagraph = new iTextParagraph(section.Title)
                    .SetFontSize(section.FontSize)
                    .SimulateBold();

                if (section.ShowBackground)
                {
                    sectionParagraph.SetBackgroundColor(template.ColorScheme.Background);
                }

                document.Add(sectionParagraph);

                if (section.Columns.Count > 0)
                {
                    GenerateTableForSection(document, data, section, template.ColorScheme);
                }
            }
        }

        private void GenerateTableContent<T>(Document document, IEnumerable<T> data, PdfDocumentTemplate template)
        {
            if (template.Columns.Count == 0) return;

            var columnWidths = template.Columns.Select(c => c.Width).ToArray();
            var table = new iTextTable(UnitValue.CreatePercentArray(columnWidths)).UseAllAvailableWidth();

            foreach (var column in template.Columns)
            {
                var headerCell = new Cell()
                    .Add(new iTextParagraph(column.DisplayName)
                        .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                        .SetFontSize(column.FontSize)
                        .SetFontColor(ColorConstants.WHITE))
                    .SetBackgroundColor(template.ColorScheme.Primary)
                    .SetTextAlignment(column.Alignment)
                    .SetBorder(Border.NO_BORDER);

                table.AddHeaderCell(headerCell);
            }

            var isEvenRow = false;
            foreach (var item in data)
            {
                foreach (var column in template.Columns)
                {
                    var value = GetPropertyValue(item, column.PropertyName);
                    var displayValue = column.ValueFormatter?.Invoke(value) ?? value?.ToString() ?? "";

                    var cell = new Cell()
                        .Add(new iTextParagraph(displayValue).SetFontSize(column.FontSize))
                        .SetTextAlignment(column.Alignment)
                        .SetBackgroundColor(isEvenRow ? template.ColorScheme.Background : ColorConstants.WHITE)
                        .SetBorder(Border.NO_BORDER);

                    if (column.IsBold)
                    {
                        cell.Add(new iTextParagraph(displayValue)
                            .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD)));
                    }

                    table.AddCell(cell);
                }
                isEvenRow = !isEvenRow;
            }

            document.Add(table);
        }

        private void GenerateTableForSection<T>(Document document, IEnumerable<T> data, PdfSectionConfig section, PdfColorScheme colorScheme)
        {
            GenerateTableContent(document, data, new PdfDocumentTemplate
            {
                Title = "",
                Columns = section.Columns,
                ColorScheme = colorScheme
            });
        }

        private static object? GetPropertyValue(object obj, string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName)) return obj;

            var properties = propertyName.Split('.');
            var currentObj = obj;

            foreach (var property in properties)
            {
                if (currentObj == null) return null;

                var propInfo = currentObj.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance);
                currentObj = propInfo?.GetValue(currentObj);
            }

            return currentObj;
        }
    }
}