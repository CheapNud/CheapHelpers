using CheapHelpers.Services.DataExchange.Pdf.Optimization;

namespace CheapHelpers.Services.DataExchange.Pdf.Configuration
{
    // PDF Optimization Configuration
    public record PdfOptimizationConfig
    {
        public string? ILovePdfApiKey { get; init; }
        public string? ILovePdfProjectId { get; init; }
        public bool UseILovePdfPrimary { get; init; } = true;
        public bool EnableITextFallback { get; init; } = true;
        public int MaxFileSizeMB { get; init; } = 50;
        public PdfOptimizationLevel DefaultLevel { get; init; } = PdfOptimizationLevel.Balanced;
    }
}
