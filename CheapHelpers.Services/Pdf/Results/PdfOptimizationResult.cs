namespace CheapHelpers.Services.Pdf.Results
{
    public record PdfOptimizationResult
    {
        public bool Success { get; init; }
        public string Method { get; init; } = "";
        public long OriginalSize { get; init; }
        public long OptimizedSize { get; init; }
        public double CompressionRatio => OriginalSize > 0 ? (double)OptimizedSize / OriginalSize : 1.0;
        public string? ErrorMessage { get; init; }
        public TimeSpan ProcessingTime { get; init; }
    }
}