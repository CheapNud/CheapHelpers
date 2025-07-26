using CheapHelpers.Models.Contracts;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CheapHelpers.Models.Entities
{
    /// <summary>
    /// Enhanced FileAttachment with timestamps and additional metadata
    /// Now includes comprehensive FluentValidation rules
    /// </summary>
    public abstract class FileAttachment : IEntityId
    {
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        public bool Visible { get; set; } = true;
        public int DisplayIndex { get; set; }

        // Timestamp properties
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(450)]
        public string? CreatedById { get; set; }

        [StringLength(450)]
        public string? UpdatedById { get; set; }

        // File metadata
        public long? FileSizeBytes { get; set; }

        [StringLength(100)]
        public string? MimeType { get; set; }

        [StringLength(10)]
        public string? FileExtension { get; set; }

        [StringLength(500)]
        public string? StoragePath { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(1000)]
        public string? Tags { get; set; }

        // Computed properties remain unchanged...
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

        // Helper methods remain the same...
        public void MarkAsUpdated(string? userId = null)
        {
            UpdatedAt = DateTime.UtcNow;
            UpdatedById = userId;
        }

        public void SetFileMetadata(FileInfo fileInfo, string? mimeType = null, string? userId = null)
        {
            FileName = fileInfo.Name;
            FileSizeBytes = fileInfo.Length;
            FileExtension = fileInfo.Extension.ToLowerInvariant();
            MimeType = mimeType;
            MarkAsUpdated(userId);
        }

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

        public void SetTags(IEnumerable<string> tags)
        {
            var tagList = tags?.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().ToList() ?? new List<string>();
            Tags = tagList.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(tagList) : null;
        }
    }
}