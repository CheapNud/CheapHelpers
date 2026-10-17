using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CheapHelpers.Helpers.Files
{
    /// <summary>
    /// A recorded set of files: relative path (forward slashes, ordinal case-sensitive) to lowercase SHA-256 hex.
    /// Serialises as <c>{"files":{relativePath: sha256hex}}</c> so other languages can produce or read it.
    /// </summary>
    public sealed class FileManifest
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        internal FileManifest(IEnumerable<KeyValuePair<string, string>> files)
        {
            var sorted = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var (path, hash) in files)
                sorted[path] = hash;

            Files = sorted;

            var lines = new StringBuilder();
            foreach (var (path, hash) in sorted)
                lines.Append(path).Append('\n').Append(hash).Append('\n');
            Fingerprint = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(lines.ToString())));
        }

        /// <summary>Relative path to lowercase SHA-256 hex, sorted ordinally by path.</summary>
        public IReadOnlyDictionary<string, string> Files { get; }

        /// <summary>SHA-256 over "path\nhash\n" lines in sorted order: one hash for the whole set, for a quick equality check before a full verify.</summary>
        public string Fingerprint { get; }

        /// <summary>Serialises the manifest as indented <c>{"files":{...}}</c> JSON.</summary>
        public string ToJson() => JsonSerializer.Serialize(new ManifestDocument(Files), JsonOptions);

        /// <summary>Reads a manifest from <c>{"files":{...}}</c> JSON. Hashes are lowercased so uppercase hex from other tools still compares equal.</summary>
        /// <exception cref="JsonException">The JSON has no files object or an entry has no hash.</exception>
        public static FileManifest FromJson(string json)
        {
            var document = JsonSerializer.Deserialize<ManifestDocument>(json);
            if (document?.Files is null)
                throw new JsonException("Manifest JSON has no \"files\" object.");

            return new FileManifest(document.Files.Select(entry => KeyValuePair.Create(
                entry.Key,
                entry.Value?.ToLowerInvariant() ?? throw new JsonException($"Manifest entry '{entry.Key}' has no hash."))));
        }

        private sealed record ManifestDocument([property: JsonPropertyName("files")] IReadOnlyDictionary<string, string>? Files);
    }

    /// <summary>Differences between a recorded manifest and the files on disk.</summary>
    /// <param name="Changed">Present in both, hash differs.</param>
    /// <param name="Missing">In the manifest, not on disk.</param>
    /// <param name="Extra">On disk, not in the manifest.</param>
    public sealed record ManifestDrift(
        IReadOnlyList<string> Changed,
        IReadOnlyList<string> Missing,
        IReadOnlyList<string> Extra)
    {
        /// <summary>True when any file changed, went missing or was added.</summary>
        public bool HasDrift => Changed.Count > 0 || Missing.Count > 0 || Extra.Count > 0;
    }

    /// <summary>
    /// Builds and verifies SHA-256 manifests of a directory tree, for deploy and install integrity checks.
    /// Files are hashed as streams, so large binaries are never loaded into memory.
    /// </summary>
    public static class FileSetFingerprint
    {
        /// <summary>Hashes every file under <paramref name="rootDirectory"/> (recursive). <paramref name="include"/> filters by relative path; null means all files.</summary>
        public static Task<FileManifest> BuildAsync(string rootDirectory, Func<string, bool>? include = null, CancellationToken cancellationToken = default)
        {
            var root = Path.GetFullPath(rootDirectory);
            var relativePaths = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Select(fullPath => ToRelative(root, fullPath))
                .Where(relativePath => include?.Invoke(relativePath) ?? true);
            return HashAsync(root, relativePaths, cancellationToken);
        }

        /// <summary>Hashes exactly these relative paths under <paramref name="rootDirectory"/>.</summary>
        /// <exception cref="FileNotFoundException">A listed file does not exist. Building a manifest from a broken set is an error; verifying one is not.</exception>
        /// <exception cref="ArgumentException">A listed path points outside <paramref name="rootDirectory"/>.</exception>
        public static Task<FileManifest> BuildAsync(string rootDirectory, IEnumerable<string> relativePaths, CancellationToken cancellationToken = default)
        {
            var root = Path.GetFullPath(rootDirectory);
            var normalised = relativePaths.Select(relativePath =>
            {
                var cleaned = ToRelative(root, Path.GetFullPath(Path.Combine(root, relativePath)));
                return cleaned == ".." || cleaned.StartsWith("../", StringComparison.Ordinal) || Path.IsPathRooted(cleaned)
                    ? throw new ArgumentException($"Path '{relativePath}' is outside the root directory.", nameof(relativePaths))
                    : cleaned;
            });
            return HashAsync(root, normalised, cancellationToken);
        }

        /// <summary>
        /// Compares the files on disk against <paramref name="recorded"/>. Always hashes; a matching version number elsewhere is no reason to skip.
        /// <paramref name="include"/> limits which on-disk files count as Extra; null means every file under <paramref name="rootDirectory"/> is considered.
        /// </summary>
        public static async Task<ManifestDrift> VerifyAsync(string rootDirectory, FileManifest recorded, Func<string, bool>? include = null, CancellationToken cancellationToken = default)
        {
            // Recorded files are always hashed, even when include would skip them, so they are never reported Missing by mistake.
            // An absent root is an empty install: every recorded file is Missing.
            var current = Directory.Exists(rootDirectory)
                ? await BuildAsync(
                    rootDirectory,
                    relativePath => recorded.Files.ContainsKey(relativePath) || (include?.Invoke(relativePath) ?? true),
                    cancellationToken)
                : new FileManifest([]);

            List<string> changed = [.. recorded.Files
                .Where(entry => current.Files.TryGetValue(entry.Key, out var hash) && hash != entry.Value)
                .Select(entry => entry.Key)];
            List<string> missing = [.. recorded.Files.Keys.Where(path => !current.Files.ContainsKey(path))];
            List<string> extra = [.. current.Files.Keys.Where(path => !recorded.Files.ContainsKey(path))];
            return new ManifestDrift(changed, missing, extra);
        }

        private static string ToRelative(string root, string fullPath) => Path.GetRelativePath(root, fullPath).Replace(Path.DirectorySeparatorChar, '/');

        private static async Task<FileManifest> HashAsync(string root, IEnumerable<string> relativePaths, CancellationToken cancellationToken)
        {
            var files = new List<KeyValuePair<string, string>>();
            foreach (var relativePath in relativePaths.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var fullPath = Path.Combine(root, relativePath);
                if (!File.Exists(fullPath))
                    throw new FileNotFoundException($"File '{relativePath}' does not exist under '{root}'.", fullPath);

                await using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
                files.Add(KeyValuePair.Create(relativePath, Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, cancellationToken))));
            }
            return new FileManifest(files);
        }
    }
}
