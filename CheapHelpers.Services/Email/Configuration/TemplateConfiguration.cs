using CheapHelpers.Helpers.Logs;
using CheapHelpers.Models.Contracts;
using CheapHelpers.Services.Email.Helpers;

namespace CheapHelpers.Services.Email.Configuration;

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
    /// Gets all built-in template data types shipped with CheapHelpers.Services
    /// </summary>
    public static Type[] GetBuiltInTemplateTypes()
    {
        return
        [
            .. GetCoreInfrastructureTypes(),
            typeof(WelcomeEmailTemplateData),
            typeof(EmailConfirmationTemplateData),
            typeof(PasswordResetTemplateData),
            typeof(ExceptionReportTemplateData),
            typeof(ExceptionReport),
            typeof(ExceptionDetails),
        ];
    }

    /// <summary>
    /// Combines core infrastructure types with user-specified template data types
    /// </summary>
    public static Type[] WithTemplateDataTypes(params Type[] templateDataTypes)
    {
        return GetCoreInfrastructureTypes()
            .Concat(templateDataTypes ?? [])
            .ToArray();
    }
}
