using CheapHelpers.Helpers.Types;
using System.Collections.Generic;

namespace CheapHelpers.Blazor.Helpers
{
    public static class Policies
    {
        /// <summary>
        /// service menu
        /// </summary>
        public const string Admin = "Admin";

        public static IEnumerable<string> GetAll()
        {
            return TypeHelper.GetStaticProperties(typeof(Policies));
        }
    }
}