namespace CheapHelpers.Services.Reporting.Storage;

/// <summary>
/// Abstraction for storing and retrieving generated report files
/// </summary>
public interface IReportStorageProvider
{
    /// <summary>
    /// Stores report content at the specified path
    /// </summary>
    Task StoreAsync(string path, byte[] content, string mimeType, CancellationToken ct = default);

    /// <summary>
    /// Retrieves report content from the specified path, or null if not found
    /// </summary>
    Task<byte[]?> GetAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// Gets a download URI for the report, optionally with an expiry for temporary access
    /// </summary>
    Task<Uri?> GetDownloadUriAsync(string path, TimeSpan? expiry = null, CancellationToken ct = default);

    /// <summary>
    /// Deletes the report at the specified path. Returns true if the file was deleted.
    /// </summary>
    Task<bool> DeleteAsync(string path, CancellationToken ct = default);
}
