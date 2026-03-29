using System.Diagnostics;

namespace CheapHelpers.Services.Reporting.Storage;

/// <summary>
/// File system-based report storage provider for local or network-attached storage
/// </summary>
public class LocalFileReportStorageProvider(string basePath) : IReportStorageProvider
{
    public async Task StoreAsync(string path, byte[] content, string mimeType, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(basePath, path);
        var directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllBytesAsync(fullPath, content, ct);
        Debug.WriteLine($"LocalFileReportStorage: Stored {content.Length} bytes to {fullPath}");
    }

    public async Task<byte[]?> GetAsync(string path, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(basePath, path);

        if (!File.Exists(fullPath))
        {
            Debug.WriteLine($"LocalFileReportStorage: File not found at {fullPath}");
            return null;
        }

        return await File.ReadAllBytesAsync(fullPath, ct);
    }

    public Task<Uri?> GetDownloadUriAsync(string path, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(basePath, path);

        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Uri?>(null);
        }

        var uri = new Uri($"file:///{fullPath.Replace('\\', '/')}");
        return Task.FromResult<Uri?>(uri);
    }

    public Task<bool> DeleteAsync(string path, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(basePath, path);

        if (!File.Exists(fullPath))
        {
            Debug.WriteLine($"LocalFileReportStorage: Nothing to delete at {fullPath}");
            return Task.FromResult(false);
        }

        File.Delete(fullPath);
        Debug.WriteLine($"LocalFileReportStorage: Deleted {fullPath}");
        return Task.FromResult(true);
    }
}
