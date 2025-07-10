using System.Reflection;

namespace CheapHelpers.Models.Contracts
{
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
}