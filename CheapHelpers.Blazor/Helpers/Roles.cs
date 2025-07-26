using CheapHelpers.Helpers.Types;

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