using CheapHelpers.Helpers.Types;
using System;
using System.Linq;

namespace CheapHelpers.Extensions
{
    public static class TypeExtensions
    {
        /// <summary>
        /// Gets an attribute of a specific type from a Type.
        /// Uses TypeAttributesCache for performance optimization.
        /// </summary>
        /// <param name="type">The type to get the attribute from</param>
        /// <param name="attributeType">The attribute type to search for</param>
        /// <returns>The attribute if found, null otherwise</returns>
        public static Attribute GetAttribute(this Type type, Type attributeType)
            => TypeAttributesCache.GetAttributes(type).FirstOrDefault(a => a.GetType() == attributeType);

        /// <summary>
        /// Gets an attribute of a specific type from a Type (generic version).
        /// Uses TypeAttributesCache for performance optimization.
        /// </summary>
        /// <typeparam name="TAttribute">The attribute type to search for</typeparam>
        /// <param name="type">The type to get the attribute from</param>
        /// <returns>The attribute if found, null otherwise</returns>
        public static TAttribute GetAttribute<TAttribute>(this Type type)
            where TAttribute : Attribute
            => (TAttribute)GetAttribute(type, typeof(TAttribute));

        /// <summary>
        /// Checks if a Type has a specific attribute.
        /// Uses TypeAttributesCache for performance optimization.
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <param name="attributeType">The attribute type to search for</param>
        /// <returns>True if the attribute exists, false otherwise</returns>
        public static bool HasAttribute(this Type type, Type attributeType)
            => GetAttribute(type, attributeType) != null;

        /// <summary>
        /// Checks if a Type has a specific attribute (generic version).
        /// Uses TypeAttributesCache for performance optimization.
        /// </summary>
        /// <typeparam name="TAttribute">The attribute type to search for</typeparam>
        /// <param name="type">The type to check</param>
        /// <returns>True if the attribute exists, false otherwise</returns>
        public static bool HasAttribute<TAttribute>(this Type type)
            where TAttribute : Attribute
            => GetAttribute<TAttribute>(type) != null;
    }
}
