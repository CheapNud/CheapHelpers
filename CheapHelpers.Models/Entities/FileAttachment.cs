using CheapHelpers.Models.Contracts;
using System.ComponentModel.DataAnnotations;

using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Enhanced FileAttachment with timestamps and additional metadata
/// </summary>
public abstract class FileAttachment : IEntityId
{
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    public bool Visible { get; set; } = true;
    public int DisplayIndex { get; set; }

    // NEW: Timestamp properties (resolves TODO)
    /// <summary>
    /// When the file was originally uploaded/created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the file metadata was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who uploaded/created the file
    /// </summary>
    [StringLength(450)] // Standard Identity user ID length
    public string? CreatedById { get; set; }

    /// <summary>
    /// Who last updated the file metadata
    /// </summary>
    [StringLength(450)]
    public string? UpdatedById { get; set; }

    // NEW: Additional file metadata
    /// <summary>
    /// File size in bytes
    /// </summary>
    public long? FileSizeBytes { get; set; }

    /// <summary>
    /// MIME type of the file
    /// </summary>
    [StringLength(100)]
    public string? MimeType { get; set; }

    /// <summary>
    /// Original file extension
    /// </summary>
    [StringLength(10)]
    public string? FileExtension { get; set; }

    /// <summary>
    /// Storage path or URL where the file is stored
    /// </summary>
    [StringLength(500)]
    public string? StoragePath { get; set; }

    /// <summary>
    /// Optional description or alt text for the file
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Tags for categorizing files (JSON array as string)
    /// </summary>
    [StringLength(1000)]
    public string? Tags { get; set; }

    // Computed properties
    [NotMapped]
    public string FileExtensionFromName =>
        !string.IsNullOrEmpty(FileName) ? Path.GetExtension(FileName).ToLowerInvariant() : string.Empty;

    [NotMapped]
    public string FileNameWithoutExtension =>
        !string.IsNullOrEmpty(FileName) ? Path.GetFileNameWithoutExtension(FileName) : string.Empty;

    [NotMapped]
    public string FileSizeFormatted => FileSizeBytes.HasValue ? FormatFileSize(FileSizeBytes.Value) : "Unknown";

    [NotMapped]
    public bool IsImage => !string.IsNullOrEmpty(MimeType) && MimeType.StartsWith("image/");

    [NotMapped]
    public bool IsDocument => !string.IsNullOrEmpty(MimeType) &&
        (MimeType.StartsWith("application/") || MimeType == "text/plain");

    // Helper methods
    /// <summary>
    /// Updates the UpdatedAt timestamp and UpdatedById
    /// </summary>
    /// <param name="userId">ID of user making the update</param>
    public void MarkAsUpdated(string? userId = null)
    {
        UpdatedAt = DateTime.UtcNow;
        UpdatedById = userId;
    }

    /// <summary>
    /// Sets file metadata from file info
    /// </summary>
    /// <param name="fileInfo">FileInfo object</param>
    /// <param name="mimeType">MIME type of the file</param>
    /// <param name="userId">User performing the operation</param>
    public void SetFileMetadata(FileInfo fileInfo, string? mimeType = null, string? userId = null)
    {
        FileName = fileInfo.Name;
        FileSizeBytes = fileInfo.Length;
        FileExtension = fileInfo.Extension.ToLowerInvariant();
        MimeType = mimeType;
        MarkAsUpdated(userId);
    }

    /// <summary>
    /// Formats file size in human-readable format
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
        double size = bytes;
        int suffixIndex = 0;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:F1} {suffixes[suffixIndex]}";
    }

    /// <summary>
    /// Gets tags as a list from the JSON string
    /// </summary>
    /// <returns>List of tags or empty list</returns>
    public List<string> GetTags()
    {
        if (string.IsNullOrEmpty(Tags))
            return new List<string>();

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(Tags) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// Sets tags from a list, serializing to JSON
    /// </summary>
    /// <param name="tags">List of tags</param>
    public void SetTags(IEnumerable<string> tags)
    {
        var tagList = tags?.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().ToList() ?? new List<string>();
        Tags = tagList.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(tagList) : null;
    }
}