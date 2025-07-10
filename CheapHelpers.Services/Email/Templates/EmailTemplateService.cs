using CheapHelpers.Models.Contracts;
using CheapHelpers.Services.Helpers;
using Fluid;
using System.Collections.Concurrent;
using System.Diagnostics;
using FluidStringValue = Fluid.Values.StringValue;

namespace CheapHelpers.Services.Email.Templates
{
    /// <summary>
    /// Minimal template service with all fixes integrated
    /// Supports Fluid/Liquid templates with enhanced DateTime handling and robust parsing
    /// </summary>
    public class EmailTemplateService
    {
        private readonly List<TemplateSource> _templateSources;
        private readonly FluidParser _parser;
        private readonly TemplateOptions _templateOptions;

        /// <summary>
        /// Cache for compiled templates
        /// </summary>
        private static readonly ConcurrentDictionary<string, IFluidTemplate> _compiledTemplates = new();

        /// <summary>
        /// Cache for partial templates (Header, Footer, etc.)
        /// </summary>
        private static readonly ConcurrentDictionary<string, string> _partialContents = new();

        private static IFluidTemplate _masterTemplate;

        /// <summary>
        /// Parameter name for body content in master template
        /// </summary>
        protected const string BodyContent = "BodyContent";

        /// <summary>
        /// Default constructor - requires explicit type registration
        /// </summary>
        public EmailTemplateService(params Type[] typesToRegister)
        {
            _templateSources = new List<TemplateSource>
            {
                new(typeof(BaseEmailTemplateData).Assembly, "CheapHelpers.Services.Templates")
            };

            _parser = new FluidParser();
            _templateOptions = new TemplateOptions();

            ConfigureTemplateOptions();
            RegisterTypes(typesToRegister);
            LoadPartials();
        }

        /// <summary>
        /// Constructor with custom template sources
        /// </summary>
        public EmailTemplateService(TemplateSource[] templateSources, params Type[] typesToRegister)
        {
            if (templateSources == null || templateSources.Length == 0)
            {
                throw new ArgumentException("At least one template source must be provided");
            }

            _templateSources = new List<TemplateSource>(templateSources);
            _parser = new FluidParser();
            _templateOptions = new TemplateOptions();

            ConfigureTemplateOptions();
            RegisterTypes(typesToRegister);
            LoadPartials();
        }

        /// <summary>
        /// Configure Fluid template options with enhanced filters and fixes
        /// </summary>
        private void ConfigureTemplateOptions()
        {
            // FIXED: Enhanced date filter that handles DateTime objects, DateTimeOffset, and ISO strings
            _templateOptions.Filters.AddFilter("date", (input, arguments, context) =>
            {
                var format = arguments.At(0).ToStringValue() ?? "dd/MM/yyyy HH:mm:ss";

                if (input == null)
                {
                    Debug.WriteLine("Date filter - Input is null");
                    return FluidStringValue.Create(string.Empty);
                }

                var value = input.ToObjectValue();
                DateTime dateTime;

                // Handle different input types - THIS IS THE KEY FIX
                if (value is DateTime dt)
                {
                    dateTime = dt;
                    Debug.WriteLine($"Date filter - Got DateTime: {dt}");
                }
                else if (value is DateTimeOffset dto)
                {
                    dateTime = dto.DateTime;
                    Debug.WriteLine($"Date filter - Got DateTimeOffset: {dto}");
                }
                else if (value is string str)
                {
                    Debug.WriteLine($"Date filter - Got string: '{str}'");

                    // Try parsing ISO format and other common formats
                    if (DateTime.TryParse(str, out var parsedDate))
                    {
                        dateTime = parsedDate;
                        Debug.WriteLine($"Date filter - Parsed successfully: {parsedDate}");
                    }
                    else if (DateTimeOffset.TryParse(str, out var parsedOffset))
                    {
                        dateTime = parsedOffset.DateTime;
                        Debug.WriteLine($"Date filter - Parsed as DateTimeOffset: {parsedOffset}");
                    }
                    else
                    {
                        Debug.WriteLine($"Date filter - Failed to parse string '{str}'");
                        return FluidStringValue.Create(str); // Return original string if parsing fails
                    }
                }
                else
                {
                    Debug.WriteLine($"Date filter - Unknown type '{value?.GetType()?.Name}' with value '{value}'");
                    return FluidStringValue.Create(value?.ToString() ?? string.Empty);
                }

                var result = dateTime.ToString(format);
                Debug.WriteLine($"Date filter - Final result: '{result}'");
                return FluidStringValue.Create(result);
            });

            // Default value filter
            _templateOptions.Filters.AddFilter("default", (input, arguments, context) =>
            {
                var value = input?.ToStringValue();
                var defaultValue = arguments.At(0).ToStringValue();
                return FluidStringValue.Create(string.IsNullOrWhiteSpace(value) ? defaultValue : value);
            });

            // Debug filter for troubleshooting
            _templateOptions.Filters.AddFilter("debug", (input, arguments, context) =>
            {
                var value = input?.ToStringValue();
                var objectValue = input?.ToObjectValue();
                Debug.WriteLine($"Debug filter - FluidStringValue: '{value}' | ObjectValue: '{objectValue}' | Type: {input?.GetType()?.Name} | ObjectType: {objectValue?.GetType()?.Name}");
                return input ?? FluidStringValue.Create("NULL");
            });

            // REMOVED: The problematic empty/not_empty filters that were being used incorrectly
            // Use Fluid's built-in truthy evaluation instead: {% if variable %}
        }

        /// <summary>
        /// Register types for template access
        /// </summary>
        private void RegisterTypes(params Type[] types)
        {
            if (types == null || types.Length == 0)
            {
                throw new ArgumentException("At least one type must be provided for registration. Use TemplateTypes.GetCoreInfrastructureTypes() for the minimal required types.");
            }

            var allTypes = types.Concat(new[] { typeof(TemplateHelpers), typeof(TemplateUrls), typeof(TemplateTheme) }).ToArray();

            foreach (var type in allTypes)
            {
                try
                {
                    _templateOptions.MemberAccessStrategy.Register(type);
                    Debug.WriteLine($"Registered type: {type.Name}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to register type {type.Name}: {ex.Message}");
                }
            }

            Debug.WriteLine($"Successfully registered {types.Length} types");
        }

        /// <summary>
        /// Load partial templates (Header, Footer, etc.)
        /// </summary>
        private void LoadPartials()
        {
            try
            {
                var partials = new[] { "Header", "Footer" };

                foreach (var partialName in partials)
                {
                    var partialContent = LoadTemplateFromSources(partialName);
                    if (!string.IsNullOrEmpty(partialContent))
                    {
                        _partialContents.TryAdd(partialName, partialContent);
                        Debug.WriteLine($"Loaded {partialName} partial");
                    }
                    else
                    {
                        Debug.WriteLine($"Warning: Could not load {partialName} partial");
                    }
                }

                // Set up file provider so Fluid can resolve includes properly
                _templateOptions.FileProvider = new EmbeddedPartialFileProvider(_partialContents);

                // Load master template after partials and file provider are set up
                LoadMasterTemplate();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load partials: {ex.Message}");
            }
        }

        /// <summary>
        /// Load master template
        /// </summary>
        private void LoadMasterTemplate()
        {
            try
            {
                var masterContent = LoadTemplateFromSources("Master");
                if (!string.IsNullOrEmpty(masterContent))
                {
                    if (_parser.TryParse(masterContent, out var masterTemplate, out var error))
                    {
                        _masterTemplate = masterTemplate;
                        Debug.WriteLine("Master template loaded successfully");
                    }
                    else
                    {
                        Debug.WriteLine($"Failed to parse master template: {error}");
                        _masterTemplate = null;
                    }
                }
                else
                {
                    Debug.WriteLine("Master template not found");
                    _masterTemplate = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load master template: {ex.Message}");
                _masterTemplate = null;
            }
        }

        /// <summary>
        /// Main method to render email templates
        /// </summary>
        public async Task<EmailTemplateResult> RenderEmailAsync<T>(T templateData) where T : IEmailTemplateData
        {
            try
            {
                if (templateData == null)
                {
                    return new EmailTemplateResult
                    {
                        IsValid = false,
                        ErrorMessage = "Template data cannot be null"
                    };
                }

                // Transform class name: Remove "TemplateData" suffix and add "TemplateBody" suffix
                var className = templateData.GetType().Name;
                var templateId = className.EndsWith("TemplateData")
                    ? className.Substring(0, className.Length - 12) + "TemplateBody"
                    : className + "TemplateBody";

                Debug.WriteLine($"Looking for template: {templateId} (from class: {className})");
                var template = GetCompiledTemplate(templateId);

                if (template == null)
                {
                    Debug.WriteLine($"Template not found: {templateId}");
                    return new EmailTemplateResult
                    {
                        IsValid = false,
                        ErrorMessage = $"Template not found: {templateId}"
                    };
                }

                // Create template context
                var context = CreateTemplateContext(templateData);

                // Render the body content
                var bodyContent = await template.RenderAsync(context);

                // If master template exists, use it; otherwise return body content directly
                if (_masterTemplate != null)
                {
                    // Add body content to context for master template
                    context.SetValue(BodyContent, bodyContent);

                    // Render the complete email using master template
                    var finalHtml = await _masterTemplate.RenderAsync(context);

                    return new EmailTemplateResult
                    {
                        Subject = templateData.Subject,
                        HtmlBody = finalHtml,
                        FromEmail = GetFromEmail(templateData),
                        FromName = GetBrandName(templateData),
                        IsValid = true
                    };
                }
                else
                {
                    // No master template - return body content directly
                    Debug.WriteLine("No master template found, returning body content directly");

                    return new EmailTemplateResult
                    {
                        Subject = templateData.Subject,
                        HtmlBody = bodyContent,
                        FromEmail = GetFromEmail(templateData),
                        FromName = GetBrandName(templateData),
                        IsValid = true
                    };
                }
            }
            catch (Exception ex)
            {
                var actualTemplateId = templateData?.GetType().Name ?? "Unknown";
                Debug.WriteLine($"Template rendering failed for {actualTemplateId}: {ex.Message}");
                return new EmailTemplateResult
                {
                    IsValid = false,
                    ErrorMessage = $"Template rendering failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Get compiled template from cache or compile if needed
        /// </summary>
        private IFluidTemplate GetCompiledTemplate(string templateId)
        {
            if (_compiledTemplates.TryGetValue(templateId, out var cachedTemplate))
            {
                return cachedTemplate;
            }

            var templateContent = LoadTemplateFromSources(templateId);
            if (string.IsNullOrEmpty(templateContent))
            {
                Debug.WriteLine($"Template not found: {templateId}");
                return null;
            }

            try
            {
                if (_parser.TryParse(templateContent, out var template, out var error))
                {
                    _compiledTemplates.TryAdd(templateId, template);
                    return template;
                }
                else
                {
                    Debug.WriteLine($"Failed to parse template {templateId}: {error}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to compile template: {templateId} - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Load template content from configured sources
        /// </summary>
        private string LoadTemplateFromSources(string templateName)
        {
            Debug.WriteLine($"=== Loading template '{templateName}' ===");

            foreach (var source in _templateSources)
            {
                try
                {
                    var resourceName = $"{source.Namespace}.{templateName}.liquid";
                    var availableResources = source.Assembly.GetManifestResourceNames();

                    var foundResource = Array.Find(availableResources, name =>
                        name.Equals(resourceName, StringComparison.OrdinalIgnoreCase));

                    if (foundResource != null)
                    {
                        using var stream = source.Assembly.GetManifestResourceStream(foundResource);
                        if (stream != null)
                        {
                            using var reader = new StreamReader(stream);
                            var content = reader.ReadToEnd();
                            Debug.WriteLine($"✓ Found template '{templateName}' in resource '{foundResource}'");
                            return content;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Failed to load template '{templateName}' from {source.Assembly.GetName().Name}: {ex.Message}");
                }
            }

            Debug.WriteLine($"❌ Template '{templateName}' not found in any source");
            return null;
        }

        /// <summary>
        /// Create template context with all necessary data and helpers
        /// FIXED: Always use original object to avoid serialization issues, with fallback for Service Fabric
        /// </summary>
        private TemplateContext CreateTemplateContext<T>(T templateData) where T : IEmailTemplateData
        {
            var context = new TemplateContext(_templateOptions);

            // FIXED: Always use the original object for local development/testing
            // The dictionary conversion was causing DateTime serialization issues
            context.SetValue("Data", templateData);

            // Create theme - simplified without external dependencies
            var theme = TemplateTheme.CreateDefault("Default Brand", "noreply@example.com");
            context.SetValue("Theme", theme);

            // Add URL settings
            var urls = new TemplateUrls
            {
                BaseUrl = "https://example.com",
                BrandImage = "",
                HelpLink = "",
                InfoEmail = ""
            };
            context.SetValue("Urls", urls);

            // Add helper values that templates can use
            var helpers = new TemplateHelpers
            {
                IsTestEnvironment = IsTestEnvironment(),
                CurrentYear = DateTime.Now.Year,
                BrandName = "Default Brand",
                DefaultEmailFrom = "noreply@example.com",
                MachineName = Environment.MachineName,
                DisplayHelp = false,
                DisplayMoreInformation = false,
            };
            context.SetValue("Helpers", helpers);

            return context;
        }

        /// <summary>
        /// Simple environment detection - customize as needed
        /// </summary>
        private static bool IsTestEnvironment()
        {
            return Environment.MachineName.Contains("DEV") ||
                   Environment.MachineName.Contains("TEST") ||
                   Debugger.IsAttached;
        }

        /// <summary>
        /// Get from email - customize as needed
        /// </summary>
        private static string GetFromEmail<T>(T templateData) where T : IEmailTemplateData
        {
            return "noreply@example.com"; // Customize this
        }

        /// <summary>
        /// Get brand name - customize as needed
        /// </summary>
        private static string GetBrandName<T>(T templateData) where T : IEmailTemplateData
        {
            return "Default Brand"; // Customize this
        }
    }
}