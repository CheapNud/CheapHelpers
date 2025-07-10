using System.Reflection;

namespace CheapHelpers.Models.Contracts
{
    /// <summary>
    /// Represents a source for loading email templates
    /// </summary>
    public class TemplateSource(Assembly assembly, string @namespace)
    {
        public Assembly Assembly { get; } = assembly ?? throw new ArgumentNullException(nameof(assembly));
        public string Namespace { get; } = @namespace ?? throw new ArgumentNullException(nameof(@namespace));

        public override string ToString()
        {
            return $"{Assembly.GetName().Name} - {Namespace}";
        }
    }
}