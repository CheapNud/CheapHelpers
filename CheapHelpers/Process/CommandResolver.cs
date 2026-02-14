using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CheapHelpers.Process;

/// <summary>
/// Cross-platform utility for resolving command names to their full executable paths.
/// Uses "where" on Windows and "which" on Linux/macOS.
/// </summary>
public static class CommandResolver
{
    /// <summary>
    /// Resolves a command name to its full path on the system PATH.
    /// Returns null if the command is not found or the resolver fails.
    /// </summary>
    /// <param name="command">The command name to resolve (e.g., "ffmpeg", "python").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The full path to the executable, or null if not found.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="command"/> is empty or contains invalid characters.</exception>
    public static async Task<string?> ResolveAsync(string command, CancellationToken cancellationToken = default)
    {
        ValidateCommand(command);

        var resolverCommand = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where" : "which";

        try
        {
            using var process = new System.Diagnostics.Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = resolverCommand,
                    Arguments = command,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
                return null;

            // "where" on Windows can return multiple lines; "which" returns one
            var firstPath = output.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            return File.Exists(firstPath) ? firstPath : null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Command resolution failed for '{command}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Resolves multiple commands in parallel and returns a dictionary of found paths.
    /// Commands that could not be resolved are omitted from the result.
    /// </summary>
    /// <param name="commands">The command names to resolve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary mapping command names to their resolved paths.</returns>
    public static async Task<Dictionary<string, string>> ResolveManyAsync(
        IEnumerable<string> commands,
        CancellationToken cancellationToken = default)
    {
        var tasks = commands.Select(async cmd =>
        {
            var resolvedPath = await ResolveAsync(cmd, cancellationToken);
            return (Command: cmd, Path: resolvedPath);
        });

        var resolvedCommands = await Task.WhenAll(tasks);

        return resolvedCommands
            .Where(r => r.Path != null)
            .ToDictionary(r => r.Command, r => r.Path!);
    }

    /// <summary>
    /// Checks whether a command exists on the system PATH without returning its path.
    /// </summary>
    /// <param name="command">The command name to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the command was found on PATH.</returns>
    public static async Task<bool> ExistsAsync(string command, CancellationToken cancellationToken = default)
    {
        return await ResolveAsync(command, cancellationToken) != null;
    }

    private static void ValidateCommand(string command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);

        // Prevent command injection via shell metacharacters
        if (command.AsSpan().IndexOfAny(['&', '|', ';', '>', '<', '`', '$', '(', ')', '{', '}', '\n', '\r']) >= 0)
            throw new ArgumentException($"Command contains invalid characters: '{command}'", nameof(command));

        // Prevent path traversal
        if (command.Contains('/') || command.Contains('\\'))
            throw new ArgumentException($"Command must be a name, not a path: '{command}'", nameof(command));
    }
}
