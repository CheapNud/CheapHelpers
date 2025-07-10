using CheapHelpers.Services.DataExchange.Pdf.Configuration;
using CheapHelpers.Services.DataExchange.Pdf.Optimization;

namespace CheapHelpers.Services.DataExchange.Pdf.Export
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