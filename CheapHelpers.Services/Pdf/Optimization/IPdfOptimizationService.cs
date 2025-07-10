using CheapHelpers.Services.Pdf.Results;

namespace CheapHelpers.Services.Pdf.Optimization
{
    public interface IPdfOptimizationService
    {
        Task<PdfOptimizationResult> OptimizeWithILovePdfAsync(string inputPath, string outputPath, PdfOptimizationLevel level);
        Task<PdfOptimizationResult> OptimizeWithILovePdfAsync(Stream input, Stream output, PdfOptimizationLevel level);
        PdfOptimizationResult OptimizeWithIText(string inputPath, string outputPath, PdfOptimizationLevel level);
        PdfOptimizationResult OptimizeWithIText(Stream input, Stream output, PdfOptimizationLevel level);
        bool IsILovePdfAvailable { get; }
    }
}