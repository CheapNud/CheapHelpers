namespace CheapHelpers.Services.Reporting.Configuration;

/// <summary>
/// Configuration options for the CheapHelpers reporting system
/// </summary>
public class ReportingOptions
{
    /// <summary>
    /// Configuration section name for binding from appsettings
    /// </summary>
    public const string SectionName = "Reporting";

    /// <summary>
    /// Default number of days to retain reports before automatic cleanup
    /// </summary>
    public int DefaultRetentionDays { get; set; } = 90;

    /// <summary>
    /// Which storage backend to use for report files
    /// </summary>
    public StorageProviderType StorageProvider { get; set; } = StorageProviderType.LocalFile;

    /// <summary>
    /// Connection string for Azure Blob Storage (required when StorageProvider is AzureBlob)
    /// </summary>
    public string? AzureBlobConnectionString { get; set; }

    /// <summary>
    /// Azure Blob Storage container name for report files
    /// </summary>
    public string BlobContainer { get; set; } = "reports";

    /// <summary>
    /// Local file system path for report storage (used when StorageProvider is LocalFile)
    /// </summary>
    public string LocalStoragePath { get; set; } = "reports";

    /// <summary>
    /// Whether to register an automatic cleanup task for expired reports
    /// </summary>
    public bool EnableCleanupTask { get; set; } = true;

    /// <summary>
    /// Time of day (UTC) to run the automatic cleanup task
    /// </summary>
    public TimeOnly CleanupRunTime { get; set; } = new(3, 0);
}

/// <summary>
/// Supported storage backends for report files
/// </summary>
public enum StorageProviderType
{
    /// <summary>
    /// Store reports in Azure Blob Storage
    /// </summary>
    AzureBlob,

    /// <summary>
    /// Store reports on the local file system
    /// </summary>
    LocalFile
}
