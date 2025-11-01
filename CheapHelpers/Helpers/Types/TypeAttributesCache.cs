using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace CheapHelpers.Helpers.Types
{
    /// <summary>
    /// Caches type attributes to avoid repeated reflection calls for better performance.
    /// </summary>
    public static class TypeAttributesCache
    {
        private static readonly ConcurrentDictionary<Type, IEnumerable<Attribute>> _classAttributes = new();

        /// <summary>
        /// Gets all custom attributes for a type, using a cache for performance.
        /// </summary>
        /// <param name="t">The type to get attributes from</param>
        /// <returns>Enumerable of attributes for the type</returns>
        public static IEnumerable<Attribute> GetAttributes(Type t)
        {
            if (!_classAttributes.ContainsKey(t))
                _classAttributes.TryAdd(t, t.GetCustomAttributes(false).Cast<Attribute>());

            return _classAttributes[t];
        }
    }
}
