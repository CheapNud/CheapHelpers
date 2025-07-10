using iText.Layout.Properties;

namespace CheapHelpers.Services.DataExchange.Pdf.Configuration
{
    public record PdfColumnConfig
    {
        public required string PropertyName { get; init; }
        public required string DisplayName { get; init; }
        public float Width { get; init; } = 1f;
        public TextAlignment Alignment { get; init; } = TextAlignment.LEFT;
        public int FontSize { get; init; } = 8;
        public bool IsBold { get; init; } = false;
        public Func<object, string>? ValueFormatter { get; init; }
    }
}