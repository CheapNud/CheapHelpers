using CheapHelpers.Services.Email.Templates;
using CheapHelpers.Services.Email.Templates.Configuration;

namespace CheapHelpers.Services.TemplateData
{
    // Usage example
    public class TemplatingExample
    {
        public async Task<string> Example()
        {
            // 1. Create template service with your template data types
            var templateService = new EmailTemplateService(
                TemplateTypes.WithTemplateDataTypes(
                    typeof(WelcomeEmailTemplateData)
                )
            );

            // 2. Create template data
            var welcomeData = new WelcomeEmailTemplateData
            {
                UserName = "John Doe",
                BrandName = "Awesome Company",
                ActivationLink = "https://example.com/activate?token=abc123",
                RegistrationDate = DateTime.Now,
                Recipient = "john.doe@example.com",
                TraceId = Guid.NewGuid().ToString()
            };

            // 3. Render the template
            var result = await templateService.RenderEmailAsync(welcomeData);

            if (result.IsValid)
            {
                Console.WriteLine($"Subject: {result.Subject}");
                Console.WriteLine($"HTML: {result.HtmlBody}");
                return result.HtmlBody;
            }
            else
            {
                Console.WriteLine($"Error: {result.ErrorMessage}");
                return null;
            }
        }
    }
}