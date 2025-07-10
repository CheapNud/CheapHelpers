using CheapHelpers.Models.Dtos.Pdf;
using CheapHelpers.Services.DataExchange.Pdf.Configuration;
using CheapHelpers.Services.DataExchange.Pdf.Infrastructure;
using iText.Pdfoptimizer;
using iText.Pdfoptimizer.Handlers;
using iText.Pdfoptimizer.Handlers.Imagequality.Processors;
using System.Diagnostics;

namespace CheapHelpers.Services.DataExchange.Pdf
{
    // PDF Optimization Service Implementation - iText only for now
    public class PdfOptimizationService : IPdfOptimizationService
    {
        private readonly PdfOptimizationConfig _config;

        public PdfOptimizationService(PdfOptimizationConfig config)
        {
            _config = config;
        }

        public bool IsILovePdfAvailable => false; // Disabled for now due to API complexity

        public async Task<PdfOptimizationResult> OptimizeWithILovePdfAsync(string inputPath, string outputPath, PdfOptimizationLevel level)
        {
            // TODO: Implement when iLovePDF API is properly understood
            await Task.CompletedTask;
            return new PdfOptimizationResult
            {
                Success = false,
                Method = "iLovePDF",
                ErrorMessage = "iLovePDF integration not yet implemented - using iText fallback"
            };
        }

        public async Task<PdfOptimizationResult> OptimizeWithILovePdfAsync(Stream input, Stream output, PdfOptimizationLevel level)
        {
            await Task.CompletedTask;
            return new PdfOptimizationResult
            {
                Success = false,
                Method = "iLovePDF",
                ErrorMessage = "iLovePDF integration not yet implemented - using iText fallback"
            };
        }

        public PdfOptimizationResult OptimizeWithIText(string inputPath, string outputPath, PdfOptimizationLevel level)
        {
            var stopwatch = Stopwatch.StartNew();
            var inputInfo = new FileInfo(inputPath);

            try
            {
                var optimizer = CreateITextOptimizer(level);
                var result = optimizer.Optimize(new FileInfo(inputPath), new FileInfo(outputPath));

                var outputInfo = new FileInfo(outputPath);
                stopwatch.Stop();

                return new PdfOptimizationResult
                {
                    Success = true,
                    Method = "iText",
                    OriginalSize = inputInfo.Length,
                    OptimizedSize = outputInfo.Length,
                    ProcessingTime = stopwatch.Elapsed
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Debug.WriteLine($"iText optimization failed: {ex.Message}");

                return new PdfOptimizationResult
                {
                    Success = false,
                    Method = "iText",
                    OriginalSize = inputInfo.Length,
                    OptimizedSize = inputInfo.Length,
                    ErrorMessage = ex.Message,
                    ProcessingTime = stopwatch.Elapsed
                };
            }
        }

        public PdfOptimizationResult OptimizeWithIText(Stream input, Stream output, PdfOptimizationLevel level)
        {
            var stopwatch = Stopwatch.StartNew();
            var originalPosition = input.Position;
            input.Position = 0;

            try
            {
                var optimizer = CreateITextOptimizer(level);
                var result = optimizer.Optimize(input, output);

                var originalSize = input.Length;
                var optimizedSize = output.Length;
                stopwatch.Stop();

                return new PdfOptimizationResult
                {
                    Success = true,
                    Method = "iText",
                    OriginalSize = originalSize,
                    OptimizedSize = optimizedSize,
                    ProcessingTime = stopwatch.Elapsed
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Debug.WriteLine($"iText stream optimization failed: {ex.Message}");

                return new PdfOptimizationResult
                {
                    Success = false,
                    Method = "iText",
                    OriginalSize = input.Length,
                    OptimizedSize = input.Length,
                    ErrorMessage = ex.Message,
                    ProcessingTime = stopwatch.Elapsed
                };
            }
            finally
            {
                input.Position = originalPosition;
            }
        }

        private static PdfOptimizer CreateITextOptimizer(PdfOptimizationLevel level)
        {
            var optimizer = new PdfOptimizer();

            optimizer.AddOptimizationHandler(new FontDuplicationOptimizer());
            optimizer.AddOptimizationHandler(new CompressionOptimizer());

            var imageOptimizer = new ImageQualityOptimizer();

            switch (level)
            {
                case PdfOptimizationLevel.Light:
                    imageOptimizer.SetJpegProcessor(new JpegCompressor(0.8f));
                    break;
                case PdfOptimizationLevel.Balanced:
                    imageOptimizer.SetJpegProcessor(new JpegCompressor(0.6f));
                    break;
                case PdfOptimizationLevel.Aggressive:
                case PdfOptimizationLevel.Maximum:
                    imageOptimizer.SetJpegProcessor(new JpegCompressor(0.4f));
                    break;
            }

            optimizer.AddOptimizationHandler(imageOptimizer);
            return optimizer;
        }
    }
}