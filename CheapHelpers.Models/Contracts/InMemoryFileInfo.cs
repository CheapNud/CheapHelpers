using Microsoft.Extensions.FileProviders;
using System.Text;

namespace CheapHelpers.Models.Contracts
{

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