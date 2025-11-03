# Email Services

Comprehensive email services with MailKit SMTP support and Fluid/Liquid templating engine for beautiful, dynamic emails.

## Table of Contents

- [Overview](#overview)
- [Available Services](#available-services)
- [Configuration](#configuration)
- [Usage Examples](#usage-examples)
- [Email Templates](#email-templates)
- [Dependency Injection Setup](#dependency-injection-setup)
- [Common Scenarios](#common-scenarios)

## Overview

The CheapHelpers.Services email package provides two main components:

1. **MailKitEmailService** - Production-ready SMTP email service using MailKit
2. **EmailTemplateService** - Fluid/Liquid template engine for dynamic email content

### Key Features

- SMTP email sending via MailKit
- HTML email support with attachments
- CC/BCC support
- Development mode (auto-redirect to developer emails)
- Fluid/Liquid templating with master layouts
- Strongly-typed template data models
- Enhanced DateTime filters
- Embedded template support
- Template caching for performance

## Available Services

### IEmailService

Main interface for sending emails:

```csharp
public interface IEmailService
{
    string[] Developers { get; }

    Task SendEmailAsync(string recipient, string subject, string body,
        (string FileName, byte[] Content)[]? attachments = null,
        string[]? cc = null, string[]? bcc = null);

    Task SendEmailAsync(string[] recipients, string subject, string body,
        (string FileName, byte[] Content)[]? attachments = null,
        string[]? cc = null, string[]? bcc = null);

    Task SendEmailAsAsync(string? from, string recipient, string subject, string body,
        (string FileName, byte[] Content)[]? attachments = null,
        string[]? cc = null, string[]? bcc = null);

    Task SendEmailAsAsync(string? from, string[] recipients, string subject, string body,
        (string FileName, byte[] Content)[]? attachments = null,
        string[]? cc = null, string[]? bcc = null);
}
```

### EmailTemplateService

Template rendering service:

```csharp
public class EmailTemplateService
{
    public EmailTemplateService(params Type[] typesToRegister);
    public EmailTemplateService(TemplateSource[] templateSources, params Type[] typesToRegister);

    public async Task<EmailTemplateResult> RenderEmailAsync<T>(T templateData)
        where T : IEmailTemplateData;
}
```

## Configuration

### MailKitEmailService Configuration

```csharp
var emailService = new MailKitEmailService(
    host: "smtp.example.com",           // SMTP server hostname
    smtpPort: 587,                      // SMTP port (587 for TLS, 465 for SSL)
    fromName: "My Application",         // Display name for sender
    fromAddress: "noreply@example.com", // From email address
    password: "your-smtp-password",     // SMTP password
    inDev: false,                       // Development mode flag
    developers: new[] { "dev@example.com" }, // Developer email addresses
    username: null,                     // Optional: SMTP username (defaults to fromAddress)
    domain: null                        // Optional: Domain for authentication
);
```

### Configuration Parameters

- **host**: SMTP server hostname (e.g., smtp.gmail.com, smtp.office365.com)
- **smtpPort**: SMTP port (587 for STARTTLS, 465 for SSL/TLS, 25 for plain)
- **fromName**: Display name shown to recipients
- **fromAddress**: Email address to send from
- **password**: SMTP authentication password
- **inDev**: When true, all emails are redirected to developer addresses
- **developers**: Array of email addresses to receive emails in development mode
- **username**: Optional SMTP username (defaults to fromAddress if not provided)
- **domain**: Optional domain for NTLM/Windows authentication

### EmailTemplateService Configuration

```csharp
using CheapHelpers.Services.Email.Configuration;

// Register template data types
var templateService = new EmailTemplateService(
    TemplateConfiguration.WithTemplateDataTypes(
        typeof(WelcomeEmailTemplateData),
        typeof(PasswordResetTemplateData),
        typeof(OrderConfirmationTemplateData)
    )
);
```

### Custom Template Sources

```csharp
var customSources = new TemplateSource[]
{
    new(typeof(MyCustomTemplate).Assembly, "MyApp.Templates")
};

var templateService = new EmailTemplateService(
    customSources,
    typeof(WelcomeEmailTemplateData)
);
```

## Usage Examples

### Basic Email Sending

```csharp
// Simple email
await emailService.SendEmailAsync(
    recipient: "user@example.com",
    subject: "Welcome!",
    body: "<h1>Welcome to our service!</h1>"
);

// Multiple recipients
await emailService.SendEmailAsync(
    recipients: new[] { "user1@example.com", "user2@example.com" },
    subject: "Team Update",
    body: "<p>Important team announcement...</p>"
);

// With CC and BCC
await emailService.SendEmailAsync(
    recipient: "primary@example.com",
    subject: "Report",
    body: "<p>Here is your report...</p>",
    cc: new[] { "manager@example.com" },
    bcc: new[] { "archive@example.com" }
);
```

### Email with Attachments

```csharp
// Prepare attachment
var pdfContent = await File.ReadAllBytesAsync("report.pdf");
var attachments = new[]
{
    ("Monthly-Report.pdf", pdfContent)
};

await emailService.SendEmailAsync(
    recipient: "user@example.com",
    subject: "Monthly Report",
    body: "<p>Please find attached your monthly report.</p>",
    attachments: attachments
);
```

### Send Email from Custom Sender

```csharp
await emailService.SendEmailAsAsync(
    from: "specific-sender@example.com",
    recipient: "user@example.com",
    subject: "Custom Sender",
    body: "<p>This email is from a specific sender</p>"
);
```

### Using Extension Methods

```csharp
// Email confirmation
await emailService.SendEmailConfirmationAsync(
    email: "newuser@example.com",
    link: "https://example.com/confirm?token=abc123"
);

// Password reset
await emailService.SendPasswordTokenAsync(
    email: "user@example.com",
    link: "https://example.com/reset?token=xyz789"
);

// Developer notifications
await emailService.SendDeveloperAsync("System initialization complete");

// Exception notification
try
{
    // ... some code
}
catch (Exception ex)
{
    await emailService.SendDeveloperAsync(ex);
}
```

### Templated Emails - Complete Example

```csharp
// 1. Create template service
var templateService = new EmailTemplateService(
    TemplateConfiguration.WithTemplateDataTypes(
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

// 4. Send the email
if (result.IsValid)
{
    await emailService.SendEmailAsync(
        recipient: welcomeData.Recipient,
        subject: result.Subject,
        body: result.HtmlBody
    );
}
else
{
    Console.WriteLine($"Template error: {result.ErrorMessage}");
}
```

## Email Templates

### Template Structure

Email templates use the Fluid/Liquid templating engine with a master layout system:

```
Templates/
├── Master.liquid          # Master layout (wraps all emails)
├── Header.liquid          # Reusable header partial
├── Footer.liquid          # Reusable footer partial
└── WelcomeEmailTemplateBody.liquid  # Template body content
```

### Master Template

The master template provides consistent structure:

```liquid
{% include 'Header' %}
{{ BodyContent }}
{% include 'Footer' %}
```

### Creating Template Data Models

All template data classes must implement `IEmailTemplateData`:

```csharp
public class WelcomeEmailTemplateData : IEmailTemplateData
{
    // IEmailTemplateData required properties
    public string Subject => "Welcome to our service!";
    public string Recipient { get; set; }
    public string TraceId { get; set; }

    // Custom properties for your template
    public string UserName { get; set; }
    public string BrandName { get; set; }
    public string ActivationLink { get; set; }
    public DateTime RegistrationDate { get; set; }
}
```

### Example Template Body

File: `WelcomeEmailTemplateBody.liquid`

```liquid
<p style='font-size: 14pt; margin: 15px 0;'>
    Hello {{ Data.UserName }}!<br><br>

    Welcome to {{ Data.BrandName }}! We're excited to have you join us.<br><br>

    Your account was created on {{ Data.RegistrationDate | date: "MMMM d, yyyy" }}.<br><br>

    To get started, please activate your account by clicking
    <a href='{{ Data.ActivationLink }}' style='color: {{ Theme.Primary | default: "#007bff" }}; text-decoration: none;'>this link</a>.<br><br>

    Happy to have you!<br><br>

    Kind regards,<br>
    Team {{ Data.BrandName }}
</p>
```

### Template Naming Convention

The template engine automatically maps template data classes to template files:

- **Class name**: `WelcomeEmailTemplateData`
- **Template file**: `WelcomeEmailTemplateBody.liquid`

**Rule**: Remove `TemplateData` suffix, append `TemplateBody` suffix.

### Available Template Variables

Templates have access to several built-in objects:

```liquid
{{ Data.PropertyName }}        <!-- Your template data -->
{{ Theme.Primary }}            <!-- Theme colors -->
{{ Theme.Secondary }}
{{ Theme.Accent }}
{{ Urls.BaseUrl }}            <!-- URL configuration -->
{{ Urls.BrandImage }}
{{ Urls.HelpLink }}
{{ Helpers.CurrentYear }}      <!-- Helper values -->
{{ Helpers.BrandName }}
{{ Helpers.IsTestEnvironment }}
```

### Available Filters

Enhanced filters for Fluid templates:

```liquid
<!-- Date formatting (enhanced to handle DateTime objects) -->
{{ Data.RegistrationDate | date: "MMMM d, yyyy" }}
{{ Data.RegistrationDate | date: "dd/MM/yyyy HH:mm:ss" }}

<!-- Default value -->
{{ Data.OptionalField | default: "No value provided" }}

<!-- Debug (outputs type information) -->
{{ Data.SomeValue | debug }}
```

### Embedding Templates in Your Assembly

1. Add liquid files to your project
2. Set Build Action to "Embedded Resource"
3. Configure namespace for templates

```xml
<ItemGroup>
  <EmbeddedResource Include="Templates\*.liquid">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </EmbeddedResource>
</ItemGroup>
```

4. Create TemplateSource:

```csharp
var source = new TemplateSource(
    assembly: typeof(MyApp).Assembly,
    namespace: "MyApp.Templates"
);
```

## Dependency Injection Setup

### ASP.NET Core Registration

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Register email service
    services.AddSingleton<IEmailService>(sp =>
        new MailKitEmailService(
            host: Configuration["Email:SmtpHost"],
            smtpPort: Configuration.GetValue<int>("Email:SmtpPort"),
            fromName: Configuration["Email:FromName"],
            fromAddress: Configuration["Email:FromAddress"],
            password: Configuration["Email:Password"],
            inDev: Environment.IsDevelopment(),
            developers: Configuration.GetSection("Email:Developers").Get<string[]>(),
            username: Configuration["Email:Username"],
            domain: Configuration["Email:Domain"]
        )
    );

    // Register template service
    services.AddSingleton(sp =>
        new EmailTemplateService(
            TemplateConfiguration.WithTemplateDataTypes(
                typeof(WelcomeEmailTemplateData),
                typeof(PasswordResetTemplateData)
            )
        )
    );
}
```

### appsettings.json Configuration

```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "FromName": "My Application",
    "FromAddress": "noreply@myapp.com",
    "Password": "your-app-specific-password",
    "Username": "noreply@myapp.com",
    "Domain": null,
    "Developers": [
      "dev1@myapp.com",
      "dev2@myapp.com"
    ]
  }
}
```

## Common Scenarios

### Scenario 1: Simple Transactional Emails

```csharp
public class OrderService
{
    private readonly IEmailService _emailService;

    public OrderService(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task SendOrderConfirmationAsync(Order order)
    {
        var body = $@"
            <h2>Order Confirmation</h2>
            <p>Order #{order.OrderNumber} has been confirmed.</p>
            <p>Total: ${order.Total:F2}</p>
        ";

        await _emailService.SendEmailAsync(
            recipient: order.CustomerEmail,
            subject: $"Order #{order.OrderNumber} Confirmed",
            body: body
        );
    }
}
```

### Scenario 2: Templated Welcome Emails

```csharp
public class UserService
{
    private readonly IEmailService _emailService;
    private readonly EmailTemplateService _templateService;

    public async Task SendWelcomeEmailAsync(User newUser)
    {
        var templateData = new WelcomeEmailTemplateData
        {
            UserName = newUser.FullName,
            BrandName = "My App",
            ActivationLink = GenerateActivationLink(newUser),
            RegistrationDate = newUser.CreatedAt,
            Recipient = newUser.Email,
            TraceId = Guid.NewGuid().ToString()
        };

        var result = await _templateService.RenderEmailAsync(templateData);

        if (result.IsValid)
        {
            await _emailService.SendEmailAsync(
                recipient: newUser.Email,
                subject: result.Subject,
                body: result.HtmlBody
            );
        }
    }
}
```

### Scenario 3: Bulk Email with Attachments

```csharp
public async Task SendMonthlyReportsAsync()
{
    var customers = await GetActiveCustomersAsync();

    foreach (var customer in customers)
    {
        var reportPdf = await GenerateReportAsync(customer);

        await _emailService.SendEmailAsync(
            recipient: customer.Email,
            subject: "Your Monthly Report",
            body: "<p>Please find your monthly report attached.</p>",
            attachments: new[] { ("Report.pdf", reportPdf) }
        );

        // Rate limiting
        await Task.Delay(100);
    }
}
```

### Scenario 4: Development Mode Testing

During development, set `inDev: true` to redirect all emails:

```csharp
var emailService = new MailKitEmailService(
    host: "smtp.example.com",
    smtpPort: 587,
    fromName: "My App",
    fromAddress: "noreply@myapp.com",
    password: "password",
    inDev: true,  // All emails redirected
    developers: new[] { "developer@myapp.com" }
);

// This will go to developer@myapp.com instead of user@example.com
await emailService.SendEmailAsync(
    recipient: "user@example.com",
    subject: "Test",
    body: "Test email"
);
```

### Scenario 5: Error Notification to Developers

```csharp
public class GlobalExceptionHandler
{
    private readonly IEmailService _emailService;

    public async Task HandleExceptionAsync(Exception ex, HttpContext context)
    {
        // Log exception
        _logger.LogError(ex, "Unhandled exception occurred");

        // Notify developers
        await _emailService.SendDeveloperAsync(ex);
    }
}
```

### Scenario 6: Multi-language Templates

```csharp
public class LocalizedTemplateService
{
    private readonly Dictionary<string, EmailTemplateService> _services = new();

    public LocalizedTemplateService()
    {
        _services["en"] = new EmailTemplateService(
            new[] { new TemplateSource(Assembly, "Templates.En") },
            typeof(WelcomeEmailTemplateData)
        );

        _services["nl"] = new EmailTemplateService(
            new[] { new TemplateSource(Assembly, "Templates.Nl") },
            typeof(WelcomeEmailTemplateData)
        );
    }

    public async Task<EmailTemplateResult> RenderAsync<T>(T data, string language)
        where T : IEmailTemplateData
    {
        var service = _services[language];
        return await service.RenderEmailAsync(data);
    }
}
```

## SMTP Provider Examples

### Gmail

```csharp
new MailKitEmailService(
    host: "smtp.gmail.com",
    smtpPort: 587,
    fromAddress: "yourapp@gmail.com",
    password: "app-specific-password",  // Generate in Google Account settings
    // ... other parameters
)
```

### Office 365 / Outlook

```csharp
new MailKitEmailService(
    host: "smtp.office365.com",
    smtpPort: 587,
    fromAddress: "noreply@yourdomain.com",
    password: "your-password",
    // ... other parameters
)
```

### SendGrid SMTP

```csharp
new MailKitEmailService(
    host: "smtp.sendgrid.net",
    smtpPort: 587,
    fromAddress: "noreply@yourdomain.com",
    username: "apikey",
    password: "your-sendgrid-api-key",
    // ... other parameters
)
```

## Best Practices

1. **Development Mode**: Always use `inDev: true` during development to avoid sending real emails
2. **Template Caching**: Templates are automatically cached for performance
3. **Error Handling**: Always check `result.IsValid` when rendering templates
4. **Rate Limiting**: Add delays when sending bulk emails to avoid SMTP throttling
5. **Async/Await**: Always use async methods for better scalability
6. **Connection Timeout**: Default is 30 seconds, suitable for most scenarios
7. **HTML Encoding**: User input in templates is automatically encoded by Fluid

## Related Documentation

- [Azure Services](Azure.md) - Azure Translation and Vision services
- [PDF Services](PDF.md) - PDF generation and optimization
- [XML Services](XML.md) - XML serialization services
