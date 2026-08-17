using CheapHelpers.Services.DataExchange.Excel;
using CheapHelpers.Services.DataExchange.Pdf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CheapHelpers.Services.DataExchange;

/// <summary>
/// DI registration for the PDF and Excel export services
/// </summary>
public static class DataExchangeServiceExtensions
{
    /// <summary>
    /// Registers <see cref="IPdfTemplateService"/> and <see cref="IPdfExportService"/>.
    /// Referenced by the reporting docs; also called by AddCheapReporting.
    /// </summary>
    public static IServiceCollection AddCheapPdfServices(this IServiceCollection services)
    {
        services.TryAddScoped<IPdfTemplateService, PdfTemplateService>();
        services.TryAddScoped<IPdfExportService, PdfExportService>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="IXlsxService"/>.
    /// </summary>
    public static IServiceCollection AddCheapXlsxServices(this IServiceCollection services)
    {
        services.TryAddScoped<IXlsxService, XlsxService>();
        return services;
    }
}
