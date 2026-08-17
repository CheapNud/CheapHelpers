using CheapHelpers.Services.DataExchange;
using CheapHelpers.Services.Reporting.Configuration;
using CheapHelpers.Services.Reporting.Distribution;
using CheapHelpers.Services.Reporting.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.Services.Reporting.Extensions;

/// <summary>
/// Extension methods for registering CheapHelpers reporting services
/// </summary>
public static class ReportingServiceExtensions
{
    /// <summary>
    /// Registers the CheapHelpers reporting system with dependency injection.
    /// Configures storage provider, report service, and distribution service.
    /// </summary>
    /// <typeparam name="TUser">The user type that inherits from IdentityUser</typeparam>
    /// <param name="services">The service collection to add services to</param>
    /// <param name="configureOptions">Optional action to configure reporting options</param>
    /// <returns>The service collection for chaining</returns>
    /// <remarks>
    /// <para>
    /// IMPORTANT: This method registers reporting services, but some features have dependencies
    /// that must be registered separately:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Report distribution requires IEmailService to be registered</description></item>
    /// <item><description>Scheduled reports require IScheduledTaskService to be registered (use AddCheapScheduling)</description></item>
    /// </list>
    /// <para>
    /// The PDF and Excel exporters (IPdfExportService, IXlsxService) are registered automatically.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddCheapReporting<TUser>(
        this IServiceCollection services,
        Action<ReportingOptions>? configureOptions = null)
        where TUser : IdentityUser
    {
        var reportingOptions = new ReportingOptions();
        configureOptions?.Invoke(reportingOptions);

        services.AddSingleton(reportingOptions);

        // Register the appropriate storage provider
        if (reportingOptions.StorageProvider == StorageProviderType.AzureBlob
            && !string.IsNullOrEmpty(reportingOptions.AzureBlobConnectionString))
        {
            services.AddSingleton<IReportStorageProvider>(
                new AzureBlobReportStorageProvider(
                    reportingOptions.AzureBlobConnectionString,
                    reportingOptions.BlobContainer));
        }
        else
        {
            services.AddSingleton<IReportStorageProvider>(
                new LocalFileReportStorageProvider(reportingOptions.LocalStoragePath));
        }

        // ReportService's ctor needs the exporters — register them here so resolving
        // IReportService can't throw at runtime (TryAdd: consumer registrations win)
        services.AddCheapPdfServices();
        services.AddCheapXlsxServices();

        // Register core reporting services
        services.AddScoped<IReportService, ReportService<TUser>>();
        services.AddScoped<IReportDistributionService, ReportDistributionService<TUser>>();

        return services;
    }
}
