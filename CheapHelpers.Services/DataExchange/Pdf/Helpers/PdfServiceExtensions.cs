using CheapHelpers.Services.DataExchange.Pdf.Configuration;

namespace CheapHelpers.Services.DataExchange.Pdf.Infrastructure
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