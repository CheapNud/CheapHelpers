using CheapHelpers.Services.Pdf.Configuration;
using CheapHelpers.Services.Pdf.Optimization;

namespace CheapHelpers.Services.Pdf.Export
{
    // Configuration helper
    public static class PdfServiceExtensions
    {
        public static PdfOptimizationConfig CreateDefaultConfig()
        {
            return new PdfOptimizationConfig
            {
                UseILovePdfPrimary = false, // Disabled until proper API integration
                EnableITextFallback = true,
                MaxFileSizeMB = 50,
                DefaultLevel = PdfOptimizationLevel.Balanced
            };
        }
    }
}