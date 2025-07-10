using CheapHelpers.Services.Pdf.Export;
using CheapHelpers.Services.Pdf.Optimization;
using CheapHelpers.Services.Pdf.Results;
using CheapHelpers.Services.Pdf.Templates;
using System.Diagnostics;
using SystemPath = System.IO.Path; // Resolve ambiguity

namespace CheapHelpers.Services.Pdf
{
    public class PdfTextService : IPdfService
    {
        private readonly IPdfExportService _exportService;
        private readonly IPdfTemplateService _templateService;
        private readonly IPdfOptimizationService _optimizationService;

        public PdfTextService(IPdfExportService exportService, IPdfTemplateService templateService, IPdfOptimizationService optimizationService)
        {
            _exportService = exportService;
            _templateService = templateService;
            _optimizationService = optimizationService;
        }

        public void Optimize(string source, string destination)
        {
            var result = OptimizeAsync(source, destination, PdfOptimizationLevel.Balanced).GetAwaiter().GetResult();
            if (!result.Success)
            {
                throw new InvalidOperationException($"PDF optimization failed: {result.ErrorMessage}");
            }
        }

        public void Optimize(Stream source, Stream destination)
        {
            var result = OptimizeAsync(source, destination, PdfOptimizationLevel.Balanced).GetAwaiter().GetResult();
            if (!result.Success)
            {
                throw new InvalidOperationException($"PDF optimization failed: {result.ErrorMessage}");
            }
        }

        public async Task<PdfOptimizationResult> OptimizeAsync(string source, string destination, PdfOptimizationLevel level = PdfOptimizationLevel.Balanced)
        {
            if (string.IsNullOrWhiteSpace(source))
                throw new ArgumentException($"'{nameof(source)}' cannot be null or whitespace.", nameof(source));
            if (string.IsNullOrWhiteSpace(destination))
                throw new ArgumentException($"'{nameof(destination)}' cannot be null or whitespace.", nameof(destination));

            PdfOptimizationResult result;

            if (_optimizationService.IsILovePdfAvailable)
            {
                Debug.WriteLine("Attempting optimization with iLovePDF...");
                result = await _optimizationService.OptimizeWithILovePdfAsync(source, destination, level);

                if (result.Success)
                {
                    Debug.WriteLine($"iLovePDF optimization successful: {result.OriginalSize:N0} → {result.OptimizedSize:N0} bytes ({result.CompressionRatio:P1} of original)");
                    return result;
                }

                Debug.WriteLine($"iLovePDF optimization failed: {result.ErrorMessage}");
            }

            Debug.WriteLine("Using iText optimization fallback...");
            result = _optimizationService.OptimizeWithIText(source, destination, level);

            if (result.Success)
            {
                Debug.WriteLine($"iText optimization successful: {result.OriginalSize:N0} → {result.OptimizedSize:N0} bytes ({result.CompressionRatio:P1} of original)");
            }
            else
            {
                Debug.WriteLine($"iText optimization failed: {result.ErrorMessage}");
            }

            return result;
        }

        public async Task<PdfOptimizationResult> OptimizeAsync(Stream source, Stream destination, PdfOptimizationLevel level = PdfOptimizationLevel.Balanced)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            PdfOptimizationResult result;

            if (_optimizationService.IsILovePdfAvailable)
            {
                Debug.WriteLine("Attempting stream optimization with iLovePDF...");
                result = await _optimizationService.OptimizeWithILovePdfAsync(source, destination, level);

                if (result.Success)
                {
                    Debug.WriteLine($"iLovePDF stream optimization successful: {result.OriginalSize:N0} → {result.OptimizedSize:N0} bytes ({result.CompressionRatio:P1} of original)");
                    return result;
                }

                Debug.WriteLine($"iLovePDF stream optimization failed: {result.ErrorMessage}");
            }

            Debug.WriteLine("Using iText stream optimization fallback...");
            result = _optimizationService.OptimizeWithIText(source, destination, level);

            if (result.Success)
            {
                Debug.WriteLine($"iText stream optimization successful: {result.OriginalSize:N0} → {result.OptimizedSize:N0} bytes ({result.CompressionRatio:P1} of original)");
            }
            else
            {
                Debug.WriteLine($"iText stream optimization failed: {result.ErrorMessage}");
            }

            return result;
        }

        // Template generation methods
        public Task GenerateOrdersAsync<T>(string filePath, IEnumerable<T> orders) where T : class
        {
            var template = PdfTemplateBuilder.CreateOrderTemplate();
            return _exportService.ExportToPdfFileAsync(orders, template, filePath);
        }

        public async Task GenerateServiceReportAsync<T>(string filePath, T service) where T : class
        {
            var template = PdfTemplateBuilder.CreateServiceTemplate();
            await _exportService.ExportToPdfFileAsync([service], template, filePath);
        }

        public Task GenerateGenericReportAsync<T>(string filePath, IEnumerable<T> data, PdfDocumentTemplate template) where T : class
        {
            return _exportService.ExportToPdfFileAsync(data, template, filePath);
        }

        public async Task GenerateAndOptimizeAsync<T>(string filePath, IEnumerable<T> data, PdfDocumentTemplate template, PdfOptimizationLevel optimizationLevel = PdfOptimizationLevel.Balanced) where T : class
        {
            var tempPath = SystemPath.GetTempFileName() + ".pdf";
            try
            {
                await _exportService.ExportToPdfFileAsync(data, template, tempPath);

                var result = await OptimizeAsync(tempPath, filePath, optimizationLevel);

                if (!result.Success)
                {
                    File.Copy(tempPath, filePath, true);
                    Debug.WriteLine($"Optimization failed, using unoptimized PDF: {result.ErrorMessage}");
                }
            }
            finally
            {
                try { File.Delete(tempPath); } catch { }
            }
        }
    }
}