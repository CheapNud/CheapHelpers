using System.Diagnostics;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace CheapHelpers.Services.Reporting.Storage;

/// <summary>
/// Azure Blob Storage-based report storage provider
/// </summary>
public class AzureBlobReportStorageProvider(string connectionString, string containerName) : IReportStorageProvider
{
    private readonly BlobServiceClient _blobServiceClient = new(connectionString);
    private BlobContainerClient? _containerClient;

    private async Task<BlobContainerClient> GetContainerClientAsync(CancellationToken ct)
    {
        if (_containerClient is not null)
        {
            return _containerClient;
        }

        _containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await _containerClient.CreateIfNotExistsAsync(cancellationToken: ct);
        return _containerClient;
    }

    public async Task StoreAsync(string path, byte[] content, string mimeType, CancellationToken ct = default)
    {
        var container = await GetContainerClientAsync(ct);
        var blobClient = container.GetBlobClient(path);

        using var stream = new MemoryStream(content);
        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = mimeType }
        };

        await blobClient.UploadAsync(stream, uploadOptions, ct);
        Debug.WriteLine($"AzureBlobReportStorage: Uploaded {content.Length} bytes to {path}");
    }

    public async Task<byte[]?> GetAsync(string path, CancellationToken ct = default)
    {
        var container = await GetContainerClientAsync(ct);
        var blobClient = container.GetBlobClient(path);

        if (!await blobClient.ExistsAsync(ct))
        {
            Debug.WriteLine($"AzureBlobReportStorage: Blob not found at {path}");
            return null;
        }

        var downloadResult = await blobClient.DownloadContentAsync(ct);
        return downloadResult.Value.Content.ToArray();
    }

    public async Task<Uri?> GetDownloadUriAsync(string path, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var container = await GetContainerClientAsync(ct);
        var blobClient = container.GetBlobClient(path);

        if (!await blobClient.ExistsAsync(ct))
        {
            return null;
        }

        if (expiry.HasValue && blobClient.CanGenerateSasUri)
        {
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = path,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.Add(expiry.Value)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            return blobClient.GenerateSasUri(sasBuilder);
        }

        return blobClient.Uri;
    }

    public async Task<bool> DeleteAsync(string path, CancellationToken ct = default)
    {
        var container = await GetContainerClientAsync(ct);
        var blobClient = container.GetBlobClient(path);

        var deleted = await blobClient.DeleteIfExistsAsync(cancellationToken: ct);
        Debug.WriteLine($"AzureBlobReportStorage: Delete {path} — {(deleted.Value ? "removed" : "not found")}");
        return deleted.Value;
    }
}
