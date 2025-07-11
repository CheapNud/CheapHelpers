using CheapHelpers.Helpers.Types;
using MoreLinq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace CheapHelpers.Blazor.Helpers
{
    public static class Roles
    {
        /// <summary>
        /// Beheerder service
        /// </summary>
        public const string Admin = "Admin";

        public static IEnumerable<string> GetAll()
        {
            return TypeHelper.GetStaticProperties(typeof(Roles));
        }
    }
}