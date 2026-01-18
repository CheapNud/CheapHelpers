using System.Diagnostics;
using CheapHelpers.Helpers.Files;

namespace CheapHelpers.MediaProcessing.Services.Utilities;

/// <summary>
/// Manages temporary files with automatic cleanup.
/// </summary>
/// <remarks>
/// <para>
/// Filename generation uses <see cref="FileHelper.GetTrustedFileName(string, DateTime?, string?)"/> which appends
/// an 8-character GUID suffix to prevent filename collisions and path traversal attacks.
/// </para>
/// <para>
/// Example: <c>CreateTempFile(".png")</c> produces <c>temp_a1b2c3d4.png</c>
/// </para>
/// <para>
/// Example: <c>GetTempFilePath("frame", ".png")</c> produces <c>frame_a1b2c3d4.png</c>
/// </para>
/// </remarks>
public class TemporaryFileManager : IDisposable
{
    private readonly List<string> _tempFiles = [];
    private readonly List<string> _tempDirectories = [];
    private readonly string _baseTempPath;
    private bool _disposed;

    public TemporaryFileManager(string? basePath = null)
    {
        _baseTempPath = basePath ?? Path.Combine(Path.GetTempPath(), "CheapMediaProcessing");
        Directory.CreateDirectory(_baseTempPath);
    }

    /// <summary>
    /// Creates a temporary file with the specified extension and returns its path.
    /// </summary>
    /// <param name="extension">File extension including the dot (e.g., ".tmp", ".png").</param>
    /// <returns>Full path to the created temporary file with format: temp_{8charGuid}{extension}</returns>
    /// <remarks>
    /// Uses <see cref="FileHelper.GetTrustedFileName(string, DateTime?, string?)"/> for secure, unique filename generation.
    /// The file is created empty and registered for automatic cleanup on disposal.
    /// </remarks>
    public string CreateTempFile(string extension = ".tmp")
    {
        var fileName = FileHelper.GetTrustedFileName($"temp{extension}");
        var filePath = Path.Combine(_baseTempPath, fileName);

        // Create empty file
        File.Create(filePath).Dispose();

        _tempFiles.Add(filePath);
        Debug.WriteLine($"[TempFile] Created: {filePath}");

        return filePath;
    }

    /// <summary>
    /// Generates a temporary file path without creating the file.
    /// </summary>
    /// <param name="extension">File extension including the dot (e.g., ".tmp", ".png").</param>
    /// <returns>Full path for a temporary file with format: temp_{8charGuid}{extension}</returns>
    /// <remarks>
    /// Uses <see cref="FileHelper.GetTrustedFileName(string, DateTime?, string?)"/> for secure, unique filename generation.
    /// The path is registered for cleanup even if the file is never created.
    /// </remarks>
    public string GetTempFilePath(string extension = ".tmp")
    {
        var fileName = FileHelper.GetTrustedFileName($"temp{extension}");
        var filePath = Path.Combine(_baseTempPath, fileName);

        _tempFiles.Add(filePath);

        return filePath;
    }

    /// <summary>
    /// Generates a temporary file path with a specific name prefix without creating the file.
    /// </summary>
    /// <param name="prefix">Prefix for the filename (e.g., "frame", "audio").</param>
    /// <param name="extension">File extension including the dot (e.g., ".tmp", ".png").</param>
    /// <returns>Full path for a temporary file with format: {prefix}_{8charGuid}{extension}</returns>
    /// <remarks>
    /// Uses <see cref="FileHelper.GetTrustedFileName(string, DateTime?, string?)"/> for secure, unique filename generation.
    /// The path is registered for cleanup even if the file is never created.
    /// </remarks>
    public string GetTempFilePath(string prefix, string extension)
    {
        var fileName = FileHelper.GetTrustedFileName($"{prefix}{extension}");
        var filePath = Path.Combine(_baseTempPath, fileName);

        _tempFiles.Add(filePath);

        return filePath;
    }

    /// <summary>
    /// Create a temporary directory
    /// </summary>
    public string CreateTempDirectory(string? name = null)
    {
        var dirName = name ?? Guid.NewGuid().ToString();
        var dirPath = Path.Combine(_baseTempPath, dirName);

        Directory.CreateDirectory(dirPath);

        _tempDirectories.Add(dirPath);
        Debug.WriteLine($"[TempDir] Created: {dirPath}");

        return dirPath;
    }

    /// <summary>
    /// Register an existing file for cleanup
    /// </summary>
    public void RegisterForCleanup(string filePath)
    {
        if (!_tempFiles.Contains(filePath))
        {
            _tempFiles.Add(filePath);
        }
    }

    /// <summary>
    /// Register an existing directory for cleanup
    /// </summary>
    public void RegisterDirectoryForCleanup(string directoryPath)
    {
        if (!_tempDirectories.Contains(directoryPath))
        {
            _tempDirectories.Add(directoryPath);
        }
    }

    /// <summary>
    /// Remove a file from cleanup tracking (e.g., if it's been moved to final destination)
    /// </summary>
    public void UnregisterFromCleanup(string filePath)
    {
        _tempFiles.Remove(filePath);
    }

    /// <summary>
    /// Clean up all tracked temporary files and directories
    /// </summary>
    public void CleanupAll()
    {
        // Delete files first
        foreach (var file in _tempFiles.ToList())
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                    Debug.WriteLine($"[TempFile] Deleted: {file}");
                }
                _tempFiles.Remove(file);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TempFile] Failed to delete {file}: {ex.Message}");
            }
        }

        // Then delete directories (in reverse order to handle nested dirs)
        foreach (var dir in _tempDirectories.OrderByDescending(d => d.Length).ToList())
        {
            try
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, recursive: true);
                    Debug.WriteLine($"[TempDir] Deleted: {dir}");
                }
                _tempDirectories.Remove(dir);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TempDir] Failed to delete {dir}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Clean up old temp files (older than specified age)
    /// </summary>
    public void CleanupOldFiles(TimeSpan maxAge)
    {
        try
        {
            if (!Directory.Exists(_baseTempPath))
                return;

            var cutoff = DateTime.Now - maxAge;

            foreach (var file in Directory.GetFiles(_baseTempPath))
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTime < cutoff)
                    {
                        File.Delete(file);
                        Debug.WriteLine($"[TempFile] Cleaned old file: {file}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[TempFile] Failed to clean {file}: {ex.Message}");
                }
            }

            foreach (var dir in Directory.GetDirectories(_baseTempPath))
            {
                try
                {
                    var dirInfo = new DirectoryInfo(dir);
                    if (dirInfo.LastWriteTime < cutoff)
                    {
                        Directory.Delete(dir, recursive: true);
                        Debug.WriteLine($"[TempDir] Cleaned old dir: {dir}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[TempDir] Failed to clean {dir}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TempFile] Cleanup failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Get total size of tracked temp files
    /// </summary>
    public long GetTotalSize()
    {
        long totalSize = 0;

        foreach (var file in _tempFiles)
        {
            try
            {
                if (File.Exists(file))
                {
                    totalSize += new FileInfo(file).Length;
                }
            }
            catch { }
        }

        return totalSize;
    }

    /// <summary>
    /// Disposes the manager and cleans up all tracked temporary files
    /// </summary>
    /// <remarks>
    /// IMPORTANT: Always dispose this manager explicitly (via 'using' or DI scope).
    /// Temp files will NOT be cleaned up automatically if Dispose is not called.
    /// This is by design - finalizers should not perform I/O operations.
    /// </remarks>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected implementation of Dispose pattern
    /// </summary>
    /// <param name="disposing">True if called from Dispose(), false if from finalizer</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Only clean up managed resources when called explicitly
            // File I/O in finalizers is dangerous and unreliable
            CleanupAll();
        }

        _disposed = true;
    }

    // NOTE: Finalizer intentionally removed.
    // File system operations in finalizers are unreliable and can cause issues.
    // Always ensure explicit disposal via 'using' statement or DI scoped lifetime.
}
