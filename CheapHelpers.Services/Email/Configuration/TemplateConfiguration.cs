using CheapHelpers.Models.Contracts;
using CheapHelpers.Services.Email.Helpers;

namespace CheapHelpers.Services.Email.Configuration
{
    /// <summary>
    /// Helper class to provide core infrastructure types for template registration
    /// </summary>
    public static class TemplateConfiguration
    {
        /// <summary>
        /// Gets the core infrastructure types needed for all email templates
        /// </summary>
        public static Type[] GetCoreInfrastructureTypes()
        {
            return
            [
                typeof(BaseEmailTemplateData),
                typeof(TemplateHelpers),
                typeof(TemplateUrls)
            ];
        }

        /// <summary>
        /// Combines core infrastructure types with user-specified template data types
        /// </summary>
        public static Type[] WithTemplateDataTypes(params Type[] templateDataTypes)
        {
            return GetCoreInfrastructureTypes()
                .Concat(templateDataTypes ?? new Type[0])
                .ToArray();
        }
    }
}
