using System.Diagnostics;
using SysProcess = System.Diagnostics.Process;

namespace CheapHelpers.Helpers;

/// <summary>
/// Path containment, collision-free file names, waiting for a file to be released, and showing a file in the OS file manager.
/// </summary>
public static class PathHelpers
{
    private static readonly TimeSpan ReadyPollInterval = TimeSpan.FromMilliseconds(250);

    /// <summary>
    /// True when <paramref name="path"/> is <paramref name="root"/> itself or lies below it after resolving <c>..</c> segments.
    /// Uses a relative path rather than a prefix check, so a sibling such as <c>/data/input-backup</c> does not count as inside <c>/data/input</c>.
    /// Symbolic links are not resolved.
    /// </summary>
    public static bool IsContainedIn(string path, string root)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentException.ThrowIfNullOrEmpty(root);

        var relative = Path.GetRelativePath(Path.GetFullPath(root), Path.GetFullPath(path));

        return relative == "."
            || (!Path.IsPathRooted(relative)
                && relative != ".."
                && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                && !relative.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal));
    }

    /// <summary>
    /// Returns <paramref name="path"/> when nothing exists there yet, otherwise the first free name of the form
    /// <c>name (2).ext</c>, <c>name (3).ext</c>, and so on. Another process can still take the name before the caller does.
    /// </summary>
    public static string GetUniqueFilePath(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        if (!Path.Exists(path))
            return path;

        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        var name = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);

        for (var counter = 2; ; counter++)
        {
            var candidate = Path.Combine(directory, $"{name} ({counter}){extension}");
            if (!Path.Exists(candidate))
                return candidate;
        }
    }

    /// <summary>
    /// Waits until the file exists and can be opened for exclusive reading, for example after another process finishes writing it.
    /// </summary>
    /// <returns>True once the file is ready; false when <paramref name="timeout"/> passes first.</returns>
    public static async Task<bool> WaitUntilReadyAsync(string path, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        var stopwatch = Stopwatch.StartNew();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
                return true;
            }
            catch (IOException)
            {
                // Missing or still locked by the writer; try again until the timeout.
            }

            var remaining = timeout - stopwatch.Elapsed;
            if (remaining <= TimeSpan.Zero)
                return false;

            await Task.Delay(remaining < ReadyPollInterval ? remaining : ReadyPollInterval, cancellationToken);
        }
    }

    /// <summary>
    /// Shows <paramref name="path"/> in the OS file manager: selected in Explorer on Windows, revealed in Finder on macOS,
    /// and its folder opened with <c>xdg-open</c> on Linux.
    /// </summary>
    /// <exception cref="PlatformNotSupportedException">On any other OS.</exception>
    public static void RevealInFileManager(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        var fullPath = Path.GetFullPath(path);

        ProcessStartInfo startInfo;
        if (OperatingSystem.IsWindows())
        {
            // Explorer parses its own command line and expects the quoted path glued to "/select,".
            startInfo = new ProcessStartInfo("explorer.exe", $"/select,\"{fullPath}\"");
        }
        else if (OperatingSystem.IsMacOS())
        {
            startInfo = new ProcessStartInfo("open") { ArgumentList = { "-R", fullPath } };
        }
        else if (OperatingSystem.IsLinux())
        {
            var folder = Directory.Exists(fullPath) ? fullPath : Path.GetDirectoryName(fullPath) ?? fullPath;
            startInfo = new ProcessStartInfo("xdg-open") { ArgumentList = { folder } };
        }
        else
        {
            throw new PlatformNotSupportedException("Revealing a file is supported on Windows, macOS and Linux.");
        }

        startInfo.UseShellExecute = false;
        using var process = SysProcess.Start(startInfo);
    }
}
