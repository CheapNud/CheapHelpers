using CheapHelpers.Services.DataExchange.Pdf.Configuration;
using iText.Kernel.Geom;

namespace CheapHelpers.Services.DataExchange.Pdf.Templates
{
    // Template configuration models
    public record PdfDocumentTemplate
    {
        public required string Title { get; init; }
        public PageSize PageSize { get; init; } = PageSize.A4;
        public bool UseHeader { get; init; } = true;
        public bool UseFooter { get; init; } = true;
        public string? HeaderTemplate { get; init; }
        public string? FooterTemplate { get; init; }
        public PdfColorScheme ColorScheme { get; init; } = PdfColorScheme.Default;
        public List<PdfColumnConfig> Columns { get; init; } = [];
        public List<PdfSectionConfig> Sections { get; init; } = [];
    }
}