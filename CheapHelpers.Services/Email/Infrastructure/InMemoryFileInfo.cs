using Microsoft.Extensions.FileProviders;
using System.Text;

namespace CheapHelpers.Services.Email.Infrastructure
{
    /// <summary>
    /// File info for in-memory template content
    /// </summary>
    public class InMemoryFileInfo(string name, string content) : IFileInfo
    {
        public bool Exists => true;
        public long Length => content.Length;
        public string PhysicalPath => null;
        public string Name { get; } = name.EndsWith(".liquid", StringComparison.OrdinalIgnoreCase)
                ? name[..^7]
                : name;
        public DateTimeOffset LastModified => DateTimeOffset.UtcNow;
        public bool IsDirectory => false;

        public Stream CreateReadStream()
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(content));
        }
    }
}