using CheapHelpers.Models.Dtos.Pdf;
using CheapHelpers.Services.DataExchange.Pdf.Configuration;
using CheapHelpers.Services.DataExchange.Pdf.Infrastructure;
using iLovePdf.Core;
using iLovePdf.Model.Enums;
using iLovePdf.Model.Task;
using iLovePdf.Model.TaskParams;
using iText.Pdfoptimizer;
using iText.Pdfoptimizer.Handlers;
using iText.Pdfoptimizer.Handlers.Imagequality.Processors;
using System.Diagnostics;

namespace CheapHelpers.Services.DataExchange.Pdf
{
    public class PdfOptimizationService : IPdfOptimizationService
    {
        private readonly PdfOptimizationConfig _config;
        private readonly iLovePdfApi? _iLovePdfApi;

        public PdfOptimizationService(PdfOptimizationConfig config)
        {
            _config = config;

            if (!string.IsNullOrEmpty(config.ILovePdfProjectId) && !string.IsNullOrEmpty(config.ILovePdfApiKey))
            {
                _iLovePdfApi = new iLovePdfApi(config.ILovePdfProjectId, config.ILovePdfApiKey);
            }
        }

        public bool IsILovePdfAvailable => _iLovePdfApi is not null;

        public async Task<PdfOptimizationResult> OptimizeWithILovePdfAsync(string inputPath, string outputPath, PdfOptimizationLevel level)
        {
            if (_iLovePdfApi is null)
                return ILovePdfNotConfiguredResult();

            var stopwatch = Stopwatch.StartNew();
            var inputInfo = new FileInfo(inputPath);

            try
            {
                var compressTask = _iLovePdfApi.CreateTask<CompressTask>();
                compressTask.AddFile(inputPath);
                compressTask.Process(new CompressParams { CompressionLevel = MapCompressionLevel(level) });
                compressTask.DownloadFile(outputPath);

                var outputInfo = new FileInfo(outputPath);
                stopwatch.Stop();

                Debug.WriteLine($"iLovePDF optimization complete: {inputInfo.Length} → {outputInfo.Length} bytes ({level})");

                return new PdfOptimizationResult
                {
                    Success = true,
                    Method = "iLovePDF",
                    OriginalSize = inputInfo.Length,
                    OptimizedSize = outputInfo.Length,
                    ProcessingTime = stopwatch.Elapsed
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Debug.WriteLine($"iLovePDF optimization failed: {ex.Message}");

                return new PdfOptimizationResult
                {
                    Success = false,
                    Method = "iLovePDF",
                    OriginalSize = inputInfo.Length,
                    ErrorMessage = ex.Message,
                    ProcessingTime = stopwatch.Elapsed
                };
            }
        }

        public async Task<PdfOptimizationResult> OptimizeWithILovePdfAsync(Stream input, Stream output, PdfOptimizationLevel level)
        {
            if (_iLovePdfApi is null)
                return ILovePdfNotConfiguredResult();

            var stopwatch = Stopwatch.StartNew();
            var originalSize = input.Length;

            try
            {
                // Read stream to byte array for upload
                input.Position = 0;
                using var memoryStream = new MemoryStream();
                await input.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                var compressTask = _iLovePdfApi.CreateTask<CompressTask>();
                compressTask.AddFile(fileBytes, "document.pdf");
                compressTask.Process(new CompressParams { CompressionLevel = MapCompressionLevel(level) });

                var resultBytes = await compressTask.DownloadFileAsByteArrayAsync();
                await output.WriteAsync(resultBytes);
                output.Position = 0;

                stopwatch.Stop();

                Debug.WriteLine($"iLovePDF stream optimization complete: {originalSize} → {resultBytes.Length} bytes ({level})");

                return new PdfOptimizationResult
                {
                    Success = true,
                    Method = "iLovePDF",
                    OriginalSize = originalSize,
                    OptimizedSize = resultBytes.Length,
                    ProcessingTime = stopwatch.Elapsed
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Debug.WriteLine($"iLovePDF stream optimization failed: {ex.Message}");

                return new PdfOptimizationResult
                {
                    Success = false,
                    Method = "iLovePDF",
                    OriginalSize = originalSize,
                    ErrorMessage = ex.Message,
                    ProcessingTime = stopwatch.Elapsed
                };
            }
        }

        private static CompressionLevels MapCompressionLevel(PdfOptimizationLevel level) => level switch
        {
            PdfOptimizationLevel.Light => CompressionLevels.Low,
            PdfOptimizationLevel.Balanced => CompressionLevels.Recommended,
            PdfOptimizationLevel.Aggressive => CompressionLevels.Extreme,
            PdfOptimizationLevel.Maximum => CompressionLevels.Extreme,
            _ => CompressionLevels.Recommended
        };

        private static PdfOptimizationResult ILovePdfNotConfiguredResult() => new()
        {
            Success = false,
            Method = "iLovePDF",
            ErrorMessage = "iLovePDF API credentials not configured (ILovePdfProjectId and ILovePdfApiKey required)"
        };

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