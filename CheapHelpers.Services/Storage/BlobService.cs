using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CheapHelpers.Models.Dtos;
using MimeMapping;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics;

namespace CheapHelpers.Services.Storage;

/// <summary>
/// Service for managing files in Azure blob containers
/// </summary>
public class BlobService(BlobServiceClient blobServiceClient)
{
    private const string PlaceholderImageUrl = "https://www.mecamgroup.com/noimageplaceholder.jpg";
    private const string PlaceholderImageFilename = "noimageplaceholder.jpg";
    private const int DefaultSasExpiryMinutes = 10;

    /// <summary>
    /// Gets the URI string for a file, returns placeholder if file doesn't exist
    /// </summary>
    public string GetFile(string? filename, BlobContainers blobContainer) =>
        string.IsNullOrWhiteSpace(filename) ? PlaceholderImageFilename : GetFileUri(filename, blobContainer).AbsoluteUri;

    /// <summary>
    /// Gets the URI string for a file attachment, returns placeholder if file doesn't exist
    /// </summary>
    public string GetFile(FileAttachment? file, BlobContainers container) =>
        file?.FileName is null ? PlaceholderImageFilename : GetFileUri(file.FileName, container).AbsoluteUri;

    /// <summary>
    /// Gets a readable stream for a file from blob storage
    /// </summary>
    public async Task<Stream?> GetFileStreamAsync(string? filename, BlobContainers blobContainer)
    {
        if (string.IsNullOrWhiteSpace(filename))
            return null;

        try
        {
            var client = GetClient(filename, blobContainer);
            var response = await client.DownloadStreamingAsync();
            return response.Value.Content;
        }
        catch (Azure.RequestFailedException ex)
        {
            Debug.WriteLine($"Failed to download blob stream '{filename}' from container '{blobContainer}': {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unexpected error downloading blob stream '{filename}': {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Downloads file content as byte array, returns null if file doesn't exist
    /// </summary>
    public async Task<byte[]?> GetFileByteArrayAsync(string? filename, BlobContainers blobContainer)
    {
        if (string.IsNullOrWhiteSpace(filename))
            return null;

        try
        {
            var client = GetClient(filename, blobContainer);
            var result = await client.DownloadContentAsync();
            return result.Value.Content.ToArray();
        }
        catch (Azure.RequestFailedException ex)
        {
            Debug.WriteLine($"Failed to download blob '{filename}' from container '{blobContainer}': {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unexpected error downloading blob '{filename}': {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Uploads a file from local filesystem to blob storage
    /// </summary>
    public async Task UploadFileAsync(string filepath, string? filename, BlobContainers container, bool overwrite = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filepath);

        filename ??= Path.GetFileName(filepath);

        using var data = File.OpenRead(filepath);
        await UploadFileAsync(data, filename, container, overwrite);
    }

    /// <summary>
    /// Uploads a file from stream to blob storage
    /// </summary>
    public async Task UploadFileAsync(Stream stream, string filename, BlobContainers container, bool overwrite = true)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(filename);

        try
        {
            var client = GetClient(filename, container);
            await client.UploadAsync(stream, overwrite);
            Debug.WriteLine($"Successfully uploaded blob '{filename}' to container '{container}'");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to upload blob '{filename}' to container '{container}': {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Deletes a file from blob storage
    /// </summary>
    public async Task DeleteFileAsync(string filename, BlobContainers container)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filename);

        try
        {
            var client = GetClient(filename, container);
            await client.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
            Debug.WriteLine($"Successfully deleted blob '{filename}' from container '{container}'");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to delete blob '{filename}' from container '{container}': {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Copies a file between blob containers
    /// </summary>
    public async Task CopyFileAsync(string filename, BlobContainers sourceContainer, BlobContainers targetContainer,
        string? newFilename = null, bool deleteOriginal = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filename);

        try
        {
            var sourceClient = GetClient(filename, sourceContainer);
            var targetFilename = newFilename ?? filename;
            var targetClient = GetClient(targetFilename, targetContainer);

            await targetClient.StartCopyFromUriAsync(sourceClient.Uri);

            if (deleteOriginal)
            {
                await sourceClient.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots);
            }

            Debug.WriteLine($"Successfully copied blob from '{sourceContainer}/{filename}' to '{targetContainer}/{targetFilename}'");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to copy blob '{filename}' from '{sourceContainer}' to '{targetContainer}': {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Gets a SAS URI for a file, returns placeholder URI if file doesn't exist
    /// </summary>
    public Uri GetFileUri(FileAttachment? fileAttachment, BlobContainers container) =>
        GetFileUri(fileAttachment?.FileName, container);

    /// <summary>
    /// Gets a SAS URI for a file, returns placeholder URI if file doesn't exist
    /// </summary>
    public Uri GetFileUri(string? filename, BlobContainers container, DateTimeOffset expires = default)
    {
        if (string.IsNullOrWhiteSpace(filename))
            return new Uri(PlaceholderImageUrl);

        if (expires == default)
            expires = DateTimeOffset.UtcNow.AddMinutes(DefaultSasExpiryMinutes);

        try
        {
            var mimeType = MimeUtility.GetMimeMapping(filename);
            var client = GetClient(filename, container);
            client.SetHttpHeaders(new BlobHttpHeaders { ContentType = mimeType });
            return client.GenerateSasUri(Azure.Storage.Sas.BlobSasPermissions.Read, expires);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to generate SAS URI for blob '{filename}': {ex.Message}");
            return new Uri(PlaceholderImageUrl);
        }
    }

    /// <summary>
    /// Deletes a file attachment from blob storage
    /// </summary>
    public async Task DeleteAttachmentAsync(FileAttachment attachment, BlobContainers container)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        await DeleteFileAsync(attachment.FileName, container);
    }

    /// <summary>
    /// Deletes multiple file attachments from blob storage
    /// </summary>
    public async Task DeleteAttachmentsAsync(IEnumerable<FileAttachment> attachments, BlobContainers container)
    {
        ArgumentNullException.ThrowIfNull(attachments);

        foreach (var file in attachments)
        {
            await DeleteFileAsync(file.FileName, container);
        }
    }

    /// <summary>
    /// Downloads image, auto-orients it, and overwrites the existing image
    /// </summary>
    public async Task CorrectImageOrientationAsync(string filename, BlobContainers container)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filename);

        try
        {
            var imageBytes = await GetFileByteArrayAsync(filename, container);
            if (imageBytes is null)
            {
                Debug.WriteLine($"Cannot correct orientation: blob '{filename}' not found in container '{container}'");
                return;
            }

            using var outStream = new MemoryStream();
            using (var image = Image.Load(imageBytes))
            {
                image.Mutate(x => x.AutoOrient());
                await image.SaveAsJpegAsync(outStream);
            }

            outStream.Position = 0;
            await UploadFileAsync(outStream, filename, container);
            Debug.WriteLine($"Successfully corrected orientation for image '{filename}'");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to correct image orientation for '{filename}': {ex.Message}");
            throw;
        }
    }

    private BlobClient GetClient(string filename, BlobContainers container)
    {
        var blobContainer = blobServiceClient.GetBlobContainerClient(container.StringValue());
        return blobContainer.GetBlobClient(filename);
    }
}