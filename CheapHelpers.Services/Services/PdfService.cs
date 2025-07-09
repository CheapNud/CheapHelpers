using iText.Commons.Actions;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Event;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Pdfoptimizer;
using iText.Pdfoptimizer.Handlers;
using iText.Pdfoptimizer.Handlers.Imagequality.Processors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using iTextParagraph = iText.Layout.Element.Paragraph; // Resolve ambiguity
using iTextRectangle = iText.Kernel.Geom.Rectangle; // Resolve ambiguity
using iTextTable = iText.Layout.Element.Table; // Resolve ambiguity
using SystemPath = System.IO.Path; // Resolve ambiguity

namespace CheapHelpers.Services
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

    public enum PdfOptimizationLevel
    {
        Light,
        Balanced,
        Aggressive,
        Maximum
    }

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

    // Template configuration models
    public record PdfDocumentTemplate
    {
        public required string Title { get; init; }
        public PageSize PageSize { get; init; } = PageSize.A4;
        public bool UseHeader { get; init; } = true;
        public bool UseFooter { get; init; } = true;
        public string? HeaderTemplate { get; init; }
        public string? FooterTemplate { get; init; }
        public PdfColorScheme ColorScheme { get; init; } = PdfColorScheme.Default;
        public List<PdfColumnConfig> Columns { get; init; } = [];
        public List<PdfSectionConfig> Sections { get; init; } = [];
    }

    public record PdfColorScheme
    {
        public DeviceRgb Primary { get; init; }
        public DeviceRgb Secondary { get; init; }
        public DeviceRgb Background { get; init; }
        public DeviceRgb Text { get; init; }
        public DeviceRgb Border { get; init; }

        public static readonly PdfColorScheme Default = new()
        {
            Primary = new DeviceRgb(116, 116, 100),
            Secondary = new DeviceRgb(144, 120, 64),
            Background = new DeviceRgb(245, 245, 245),
            Text = new DeviceRgb(51, 51, 51),
            Border = new DeviceRgb(200, 200, 200)
        };

        public static readonly PdfColorScheme Sober = new()
        {
            Primary = new DeviceRgb(188, 224, 255),
            Secondary = new DeviceRgb(119, 119, 119),
            Background = new DeviceRgb(211, 211, 211),
            Text = new DeviceRgb(0, 0, 0),
            Border = new DeviceRgb(200, 200, 200)
        };
    }

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

    public record PdfSectionConfig
    {
        public required string Title { get; init; }
        public string? PropertyName { get; init; }
        public int FontSize { get; init; } = 12;
        public bool ShowBackground { get; init; } = true;
        public List<PdfColumnConfig> Columns { get; init; } = [];
    }


    // Template service interfaces
    public interface IPdfTemplateService
    {
        void AddHeader(Document document, string? template, PdfColorScheme colorScheme);
        void AddFooter(PdfDocument pdfDocument, string? template, PdfColorScheme colorScheme);
        IEventHandler CreateHeaderFooterHandler(string? headerTemplate, string? footerTemplate, PdfColorScheme colorScheme);
    }

    public interface IPdfExportService
    {
        Task<byte[]> ExportToPdfAsync<T>(IEnumerable<T> data, PdfDocumentTemplate template);
        Task ExportToPdfFileAsync<T>(IEnumerable<T> data, PdfDocumentTemplate template, string filePath);
        Task<byte[]> ExportSingleToPdfAsync<T>(T entity, PdfDocumentTemplate template);
    }

    public interface IPdfOptimizationService
    {
        Task<PdfOptimizationResult> OptimizeWithILovePdfAsync(string inputPath, string outputPath, PdfOptimizationLevel level);
        Task<PdfOptimizationResult> OptimizeWithILovePdfAsync(Stream input, Stream output, PdfOptimizationLevel level);
        PdfOptimizationResult OptimizeWithIText(string inputPath, string outputPath, PdfOptimizationLevel level);
        PdfOptimizationResult OptimizeWithIText(Stream input, Stream output, PdfOptimizationLevel level);
        bool IsILovePdfAvailable { get; }
    }

    // Enhanced event handler for templated headers/footers  
    public class TemplatedHeaderFooterHandler : AbstractPdfDocumentEventHandler
    {
        private readonly string? _headerTemplate;
        private readonly string? _footerTemplate;
        private readonly PdfColorScheme _colorScheme;

        public TemplatedHeaderFooterHandler(string? headerTemplate, string? footerTemplate, PdfColorScheme colorScheme)
        {
            _headerTemplate = headerTemplate;
            _footerTemplate = footerTemplate;
            _colorScheme = colorScheme;
        }

        protected override void OnAcceptedEvent(AbstractPdfDocumentEvent currentEvent)
        {
            if (currentEvent is PdfDocumentEvent docEvent)
            {
                var pdfDoc = docEvent.GetDocument();
                var page = docEvent.GetPage();
                var pageSize = page.GetPageSize();
                var canvas = new PdfCanvas(page.NewContentStreamBefore(), page.GetResources(), pdfDoc);

                try
                {
                    var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                    // Draw footer
                    if (!string.IsNullOrWhiteSpace(_footerTemplate))
                    {
                        DrawFooter(canvas, pageSize, font);
                    }

                    // Draw header 
                    if (!string.IsNullOrWhiteSpace(_headerTemplate))
                    {
                        DrawHeader(canvas, pageSize, font);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in template handler: {ex.Message}");
                }
            }
        }

        private void DrawFooter(PdfCanvas canvas, iTextRectangle pageSize, PdfFont font)
        {
            const float FOOTER_HEIGHT = 30f;
            const float MARGIN = 20f;

            canvas.SaveState()
                .SetStrokeColor(_colorScheme.Border)
                .SetLineWidth(0.5f)
                .MoveTo(pageSize.GetLeft() + MARGIN, pageSize.GetBottom() + FOOTER_HEIGHT)
                .LineTo(pageSize.GetRight() - MARGIN, pageSize.GetBottom() + FOOTER_HEIGHT)
                .Stroke();

            var footerText = _footerTemplate ?? "____________________________________________";
            var textWidth = font.GetWidth(footerText, 7);
            var xPosition = (pageSize.GetLeft() + pageSize.GetRight() - textWidth) / 2;

            canvas.BeginText()
                .SetFontAndSize(font, 7)
                .SetColor(_colorScheme.Secondary, true)
                .MoveText(xPosition, pageSize.GetBottom() + 15)
                .ShowText(footerText)
                .EndText()
                .RestoreState();
        }

        private void DrawHeader(PdfCanvas canvas, iTextRectangle pageSize, PdfFont font)
        {
            const float HEADER_HEIGHT = 50f;

            var headerText = _headerTemplate ?? "";
            if (!string.IsNullOrWhiteSpace(headerText))
            {
                var textWidth = font.GetWidth(headerText, 10);
                var xPosition = (pageSize.GetLeft() + pageSize.GetRight() - textWidth) / 2;

                canvas.BeginText()
                    .SetFontAndSize(font, 10)
                    .SetColor(_colorScheme.Primary, true)
                    .MoveText(xPosition, pageSize.GetTop() - HEADER_HEIGHT)
                    .ShowText(headerText)
                    .EndText();
            }
        }
    }

    // Template service implementation
    public class PdfTemplateService : IPdfTemplateService
    {
        private const string COMPANY_NAME = "COMPANY_NAME";
        private const string COMPANY_ADDRESS = "COMPANY_ADDRESS";
        private const string COMPANY_VAT = "COMPANY_VAT";

        public void AddHeader(Document document, string? template, PdfColorScheme colorScheme)
        {
            var headerTable = new iTextTable([2f, 1f]).UseAllAvailableWidth();

            var companyCell = new Cell()
                .Add(new iTextParagraph(COMPANY_NAME).SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD)))
                .Add(new iTextParagraph(COMPANY_ADDRESS).SetFontSize(10))
                .Add(new iTextParagraph(COMPANY_VAT).SetFontSize(10))
                .SetBorder(Border.NO_BORDER);

            headerTable.AddCell(companyCell);

            var rightCell = new Cell()
                .Add(new iTextParagraph(template ?? DateTime.Now.ToString("dd/MM/yyyy")).SetFontSize(10))
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetBorder(Border.NO_BORDER);

            headerTable.AddCell(rightCell);
            document.Add(headerTable.SetMarginBottom(10));
        }

        public void AddFooter(PdfDocument pdfDocument, string? template, PdfColorScheme colorScheme)
        {
            var handler = CreateHeaderFooterHandler(null, template, colorScheme);
            pdfDocument.AddEventHandler(PdfDocumentEvent.END_PAGE, handler as AbstractPdfDocumentEventHandler);
        }

        public IEventHandler CreateHeaderFooterHandler(string? headerTemplate, string? footerTemplate, PdfColorScheme colorScheme)
        {
            return new TemplatedHeaderFooterHandler(headerTemplate, footerTemplate, colorScheme);
        }
    }

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

    // Generic export service implementation
    public class PdfExportService : IPdfExportService
    {
        private readonly IPdfTemplateService _templateService;

        public PdfExportService(IPdfTemplateService templateService)
        {
            _templateService = templateService;
        }

        public async Task<byte[]> ExportToPdfAsync<T>(IEnumerable<T> data, PdfDocumentTemplate template)
        {
            using var memoryStream = new MemoryStream();
            await GeneratePdfAsync(data, template, memoryStream);
            return memoryStream.ToArray();
        }

        public async Task ExportToPdfFileAsync<T>(IEnumerable<T> data, PdfDocumentTemplate template, string filePath)
        {
            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            await GeneratePdfAsync(data, template, fileStream);
        }

        public async Task<byte[]> ExportSingleToPdfAsync<T>(T entity, PdfDocumentTemplate template)
        {
            return await ExportToPdfAsync([entity], template);
        }

        private async Task GeneratePdfAsync<T>(IEnumerable<T> data, PdfDocumentTemplate template, Stream stream)
        {
            await Task.Run(() =>
            {
                using var writer = new PdfWriter(stream);
                using var pdfDocument = new PdfDocument(writer);
                pdfDocument.SetDefaultPageSize(template.PageSize);

                var document = new Document(pdfDocument);
                var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                document.SetFont(font);

                if (template.UseHeader || template.UseFooter)
                {
                    var handler = _templateService.CreateHeaderFooterHandler(
                        template.UseHeader ? template.HeaderTemplate : null,
                        template.UseFooter ? template.FooterTemplate : null,
                        template.ColorScheme);
                    pdfDocument.AddEventHandler(PdfDocumentEvent.END_PAGE, handler as AbstractPdfDocumentEventHandler);
                }

                if (!string.IsNullOrWhiteSpace(template.Title))
                {
                    document.Add(new iTextParagraph(template.Title)
                        .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                        .SetFontSize(16)
                        .SetFontColor(template.ColorScheme.Primary)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginBottom(10));
                }

                if (template.Sections.Count > 0)
                {
                    GenerateSectionedContent(document, data, template);
                }
                else
                {
                    GenerateTableContent(document, data, template);
                }

                document.Close();
            });
        }

        private void GenerateSectionedContent<T>(Document document, IEnumerable<T> data, PdfDocumentTemplate template)
        {
            foreach (var section in template.Sections)
            {
                var sectionParagraph = new iTextParagraph(section.Title)
                    .SetFontSize(section.FontSize)
                    .SimulateBold();

                if (section.ShowBackground)
                {
                    sectionParagraph.SetBackgroundColor(template.ColorScheme.Background);
                }

                document.Add(sectionParagraph);

                if (section.Columns.Count > 0)
                {
                    GenerateTableForSection(document, data, section, template.ColorScheme);
                }
            }
        }

        private void GenerateTableContent<T>(Document document, IEnumerable<T> data, PdfDocumentTemplate template)
        {
            if (template.Columns.Count == 0) return;

            var columnWidths = template.Columns.Select(c => c.Width).ToArray();
            var table = new iTextTable(UnitValue.CreatePercentArray(columnWidths)).UseAllAvailableWidth();

            foreach (var column in template.Columns)
            {
                var headerCell = new Cell()
                    .Add(new iTextParagraph(column.DisplayName)
                        .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                        .SetFontSize(column.FontSize)
                        .SetFontColor(ColorConstants.WHITE))
                    .SetBackgroundColor(template.ColorScheme.Primary)
                    .SetTextAlignment(column.Alignment)
                    .SetBorder(Border.NO_BORDER);

                table.AddHeaderCell(headerCell);
            }

            var isEvenRow = false;
            foreach (var item in data)
            {
                foreach (var column in template.Columns)
                {
                    var value = GetPropertyValue(item, column.PropertyName);
                    var displayValue = column.ValueFormatter?.Invoke(value) ?? value?.ToString() ?? "";

                    var cell = new Cell()
                        .Add(new iTextParagraph(displayValue).SetFontSize(column.FontSize))
                        .SetTextAlignment(column.Alignment)
                        .SetBackgroundColor(isEvenRow ? template.ColorScheme.Background : ColorConstants.WHITE)
                        .SetBorder(Border.NO_BORDER);

                    if (column.IsBold)
                    {
                        cell.Add(new iTextParagraph(displayValue)
                            .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD)));
                    }

                    table.AddCell(cell);
                }
                isEvenRow = !isEvenRow;
            }

            document.Add(table);
        }

        private void GenerateTableForSection<T>(Document document, IEnumerable<T> data, PdfSectionConfig section, PdfColorScheme colorScheme)
        {
            GenerateTableContent(document, data, new PdfDocumentTemplate
            {
                Title = "",
                Columns = section.Columns,
                ColorScheme = colorScheme
            });
        }

        private static object? GetPropertyValue(object obj, string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName)) return obj;

            var properties = propertyName.Split('.');
            var currentObj = obj;

            foreach (var property in properties)
            {
                if (currentObj == null) return null;

                var propInfo = currentObj.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance);
                currentObj = propInfo?.GetValue(currentObj);
            }

            return currentObj;
        }
    }

    // Template builder for easy configuration
    public static class PdfTemplateBuilder
    {
        public static PdfDocumentTemplate CreateOrderTemplate()
        {
            return new PdfDocumentTemplate
            {
                Title = "Orders",
                PageSize = PageSize.A4.Rotate(),
                UseFooter = true,
                FooterTemplate = "Orders Export",
                ColorScheme = PdfColorScheme.Sober,
                Columns =
                [
                    new() { PropertyName = "OrderNumber", DisplayName = "Ordernummer", Width = 1f },
                    new() { PropertyName = "CreationDate", DisplayName = "Creatie Datum", Width = 1f, ValueFormatter = obj => ((DateTime)obj).ToShortDateString() },
                    new() { PropertyName = "DeliveryDate", DisplayName = "Lever Datum", Width = 1f, ValueFormatter = obj => ((DateTime)obj).ToShortDateString() },
                    new() { PropertyName = "VblCode", DisplayName = "Vblnummer", Width = 1f },
                    new() { PropertyName = "OldModelCode", DisplayName = "Model", Width = 1f },
                    new() { PropertyName = "Fabrics", DisplayName = "Stof", Width = 1f },
                    new() { PropertyName = "Customer.Code", DisplayName = "Klant", Width = 1f }
                ]
            };
        }

        public static PdfDocumentTemplate CreateServiceTemplate()
        {
            return new PdfDocumentTemplate
            {
                Title = "Service Rapport",
                UseHeader = true,
                UseFooter = true,
                ColorScheme = PdfColorScheme.Default,
                Sections =
                [
                    new PdfSectionConfig
                    {
                        Title = "Service Details",
                        Columns =
                        [
                            new() { PropertyName = "ServiceNumber", DisplayName = "Service Nr", Width = 1f },
                            new() { PropertyName = "Customer.Name", DisplayName = "Klant", Width = 2f },
                            new() { PropertyName = "ExecutionDate", DisplayName = "Uitvoering", Width = 1f }
                        ]
                    }
                ]
            };
        }
    }

    // Updated main service 
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