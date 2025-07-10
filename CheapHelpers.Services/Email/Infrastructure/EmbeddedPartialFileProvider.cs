using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System.Collections.Concurrent;

namespace CheapHelpers.Services.Email.Infrastructure
{
    /// <summary>
    /// Custom file provider for Fluid templates that serves partials from memory
    /// Fluid needs this to be able to "include" templates without file system access.
    /// </summary>
    public class EmbeddedPartialFileProvider : IFileProvider
    {
        private readonly ConcurrentDictionary<string, string> _partials;

        public EmbeddedPartialFileProvider(ConcurrentDictionary<string, string> partials)
        {
            _partials = partials;
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            // Remove leading slash if present and normalize the path
            var normalizedPath = subpath.TrimStart('/');

            // First try exact match
            if (_partials.TryGetValue(normalizedPath, out var content))
            {
                return new InMemoryFileInfo(normalizedPath, content);
            }

            // If no exact match, try with .liquid extension stripped
            var pathWithoutExtension = normalizedPath;
            if (pathWithoutExtension.EndsWith(".liquid", StringComparison.OrdinalIgnoreCase))
            {
                pathWithoutExtension = pathWithoutExtension.Substring(0, pathWithoutExtension.Length - 7);
            }

            if (_partials.TryGetValue(pathWithoutExtension, out content))
            {
                return new InMemoryFileInfo(pathWithoutExtension, content);
            }

            // Try with .liquid extension added
            var pathWithExtension = normalizedPath + ".liquid";
            if (_partials.TryGetValue(pathWithExtension, out content))
            {
                return new InMemoryFileInfo(normalizedPath, content);
            }

            return new NotFoundFileInfo(normalizedPath);
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            return NotFoundDirectoryContents.Singleton;
        }

        public IChangeToken Watch(string filter)
        {
            return NullChangeToken.Singleton;
        }
    }
}