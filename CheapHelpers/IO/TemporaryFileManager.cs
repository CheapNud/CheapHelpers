using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace CheapHelpers.IO;

/// <summary>
/// Manages temporary files and directories with automatic cleanup
/// Ensures cleanup even on exceptions or cancellation
/// </summary>
public class TemporaryFileManager : IDisposable
{
    private readonly string _baseDirectory;
    private readonly List<string> _trackedDirectories = [];
    private readonly List<string> _trackedFiles = [];
    private bool _disposed;

    /// <summary>
    /// Base directory for all temporary files
    /// </summary>
    public string BaseDirectory => _baseDirectory;

    public TemporaryFileManager(string? customBaseDirectory = null, string applicationName = "CheapHelpers")
    {
        _baseDirectory = customBaseDirectory ?? Path.Combine(
            Path.GetTempPath(),
            applicationName,
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(_baseDirectory);
        Debug.WriteLine($"Created temporary directory: {_baseDirectory}");
    }

    /// <summary>
    /// Create a temporary subdirectory
    /// </summary>
    public string CreateTempDirectory(string subfolder)
    {
        var directoryPath = Path.Combine(_baseDirectory, subfolder);
        Directory.CreateDirectory(directoryPath);
        _trackedDirectories.Add(directoryPath);
        Debug.WriteLine($"Created temp subdirectory: {directoryPath}");
        return directoryPath;
    }

    /// <summary>
    /// Create a temporary file path
    /// </summary>
    public string CreateTempFile(string filename)
    {
        var filePath = Path.Combine(_baseDirectory, filename);
        _trackedFiles.Add(filePath);
        return filePath;
    }

    /// <summary>
    /// Get a path within the temporary directory
    /// </summary>
    public string GetTempPath(string relativePath)
    {
        return Path.Combine(_baseDirectory, relativePath);
    }

    /// <summary>
    /// Clean up all tracked files and directories
    /// </summary>
    public void Cleanup()
    {
        if (_disposed)
            return;

        Debug.WriteLine("Cleaning up temporary files...");

        // Delete tracked files
        foreach (var file in _trackedFiles)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                    Debug.WriteLine($"Deleted file: {file}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to delete file {file}: {ex.Message}");
            }
        }

        // Delete tracked directories
        foreach (var directory in _trackedDirectories)
        {
            try
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, recursive: true);
                    Debug.WriteLine($"Deleted directory: {directory}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to delete directory {directory}: {ex.Message}");
            }
        }

        // Delete base directory
        try
        {
            if (Directory.Exists(_baseDirectory))
            {
                Directory.Delete(_baseDirectory, recursive: true);
                Debug.WriteLine($"Deleted base directory: {_baseDirectory}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to delete base directory {_baseDirectory}: {ex.Message}");
        }

        _trackedFiles.Clear();
        _trackedDirectories.Clear();
    }

    /// <summary>
    /// Get directory size in bytes
    /// </summary>
    public long GetDirectorySize(string path)
    {
        if (!Directory.Exists(path))
            return 0;

        try
        {
            return new DirectoryInfo(path)
                .GetFiles("*", SearchOption.AllDirectories)
                .Sum(file => file.Length);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Get human-readable size
    /// </summary>
    public static string FormatSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        var order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:F2} {sizes[order]}";
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Cleanup();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~TemporaryFileManager()
    {
        Dispose();
    }
}
