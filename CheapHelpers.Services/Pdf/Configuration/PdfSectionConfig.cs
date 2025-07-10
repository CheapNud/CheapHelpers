namespace CheapHelpers.Services.Pdf.Configuration
{
    public record PdfSectionConfig
    {
        public required string Title { get; init; }
        public string? PropertyName { get; init; }
        public int FontSize { get; init; } = 12;
        public bool ShowBackground { get; init; } = true;
        public List<PdfColumnConfig> Columns { get; init; } = [];
    }
}