using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;

namespace CheapHelpers.Models.Contracts
{
    /// <summary>
    /// Base interface for all email template data
    /// </summary>
    public interface IEmailTemplateData
    {
        string Subject { get; }
        string Recipient { get; set; }
        string TraceId { get; set; }
    }

    /// <summary>
    /// Base implementation for email template data
    /// </summary>
    public abstract class BaseEmailTemplateData : IEmailTemplateData
    {
        public abstract string Subject { get; }
        public string Recipient { get; set; }
        public string TraceId { get; set; }
    }

    /// <summary>
    /// Result of template rendering
    /// </summary>
    public class EmailTemplateResult
    {
        public string Subject { get; set; }
        public string HtmlBody { get; set; }
        public string TextBody { get; set; }
        public string FromEmail { get; set; }
        public string FromName { get; set; }
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Represents a source for loading email templates
    /// </summary>
    public class TemplateSource
    {
        public Assembly Assembly { get; }
        public string Namespace { get; }

        public TemplateSource(Assembly assembly, string @namespace)
        {
            Assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
            Namespace = @namespace ?? throw new ArgumentNullException(nameof(@namespace));
        }

        public override string ToString()
        {
            return $"{Assembly.GetName().Name} - {Namespace}";
        }
    }

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

    /// <summary>
    /// File info for in-memory template content
    /// </summary>
    public class InMemoryFileInfo : IFileInfo
    {
        private readonly string _content;

        public InMemoryFileInfo(string name, string content)
        {
            // Ensure the name doesn't have .liquid extension for Fluid compatibility
            Name = name.EndsWith(".liquid", StringComparison.OrdinalIgnoreCase)
                ? name.Substring(0, name.Length - 7)
                : name;
            _content = content;
        }

        public bool Exists => true;
        public long Length => _content.Length;
        public string PhysicalPath => null;
        public string Name { get; }
        public DateTimeOffset LastModified => DateTimeOffset.UtcNow;
        public bool IsDirectory => false;

        public Stream CreateReadStream()
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(_content));
        }
    }
}