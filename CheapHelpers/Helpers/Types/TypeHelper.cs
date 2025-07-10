using CheapHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CheapHelpers.Helpers.Types
{
    public static class TypeHelper
    {
        public static IEnumerable<string> GetStaticProperties(Type type)
        {
            return type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy).Where(fi => fi.IsLiteral && !fi.IsInitOnly).Select(x => x.Name).ToList();
        }
    }
}
