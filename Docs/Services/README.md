# CheapHelpers.Services Documentation

Business services including email with templates, PDF generation, Azure integration, and document processing.

## Package Information

- **Package**: CheapHelpers.Services
- **Version**: 1.1.3
- **Target Framework**: .NET 10.0
- **NuGet**: [CheapHelpers.Services](https://www.nuget.org/packages/CheapHelpers.Services)

## Available Services

### [Geocoding Services](Geocoding.md)
Unified geocoding interface with support for 4 major providers.

**Features:**
- Forward geocoding (address to coordinates)
- Reverse geocoding (coordinates to address)
- Fuzzy search/autocomplete
- Multi-provider support (Mapbox, Azure Maps, Google Maps, PTV Maps)
- Unified models and options
- Factory pattern for provider switching

**Key Classes:**
- `IGeocodingService` - Main geocoding interface
- `IGeocodingServiceFactory` - Provider factory
- `MapboxGeocodingService`, `AzureMapsGeocodingService`, `GoogleMapsGeocodingService`, `PtvMapsGeocodingService`

### [Email Services](Email.md)
Complete email solution with MailKit SMTP and Fluid templating.

**Features:**
- SMTP email sending via MailKit
- HTML email with attachments
- Fluid/Liquid templating engine
- Master layouts with partials
- Development mode redirection
- CC/BCC support

**Key Classes:**
- `MailKitEmailService` - SMTP email service
- `EmailTemplateService` - Template rendering

### [PDF Services](PDF.md)
PDF generation, templating, and optimization using iText and iLovePDF.

**Features:**
- Template-based PDF generation
- Data-driven tables and sections
- PDF optimization (iLovePDF + iText fallback)
- Custom color schemes
- Header/footer support
- Multi-column layouts

**Key Classes:**
- `PdfService` - Main PDF operations
- `PdfExportService` - Data to PDF export
- `PdfOptimizationService` - PDF compression
- `PdfTemplateBuilder` - Pre-built templates

### [XML Services](XML.md)
Dynamic and strongly-typed XML serialization.

**Features:**
- Dynamic object to XML conversion
- Strongly-typed XmlSerializer support
- File and string operations
- Nested object support
- Element name sanitization
- Collection handling

**Key Classes:**
- `XmlService` - Implements `IXmlService`

### [Azure Services](Azure.md)
Azure Cognitive Services integration for Translation and Vision.

**Features:**
- Text translation (19+ languages)
- Batch translation
- Language detection
- Document translation
- Image analysis
- OCR text extraction
- Object detection

**Key Classes:**
- `TranslatorService` - Azure Translator API
- `AzureVisionService` - Computer Vision API

## Quick Start

### Installation

```bash
dotnet add package CheapHelpers.Services
```

### Basic Usage Examples

#### Geocoding

```csharp
using CheapHelpers.Services.Geocoding.Extensions;

// Configure services
builder.Services.AddGeocodingServices(options =>
{
    options.DefaultProvider = GeocodingProvider.Mapbox;
    options.Mapbox.AccessToken = "your-mapbox-token";
    options.AzureMaps.SubscriptionKey = "your-azure-key";
    options.AzureMaps.ClientId = "your-client-id";
    options.GoogleMaps.ApiKey = "your-google-key";
    options.PtvMaps.ApiKey = "your-ptv-key";
});

// Forward geocoding
var result = await geocodingService.GeocodeAsync(
    "1600 Amphitheatre Parkway, Mountain View, CA");
Console.WriteLine($"Coordinates: {result.Coordinate.Latitude}, {result.Coordinate.Longitude}");

// Reverse geocoding
var address = await geocodingService.ReverseGeocodeAsync(37.4224764, -122.0842499);
Console.WriteLine($"Address: {address.FormattedAddress}");

// Fuzzy search
var results = await geocodingService.SearchAsync("main st", new GeocodingOptions
{
    Limit = 5,
    Countries = new[] { "US" }
});
```

#### Email

```csharp
var emailService = new MailKitEmailService(
    host: "smtp.gmail.com",
    smtpPort: 587,
    fromName: "My App",
    fromAddress: "noreply@myapp.com",
    password: "app-password",
    inDev: false,
    developers: new[] { "dev@myapp.com" }
);

await emailService.SendEmailAsync(
    recipient: "user@example.com",
    subject: "Welcome!",
    body: "<h1>Welcome to our service!</h1>"
);
```

#### PDF

```csharp
var template = PdfTemplateBuilder.CreateOrderTemplate();
var orders = await GetOrdersAsync();
await pdfExportService.ExportToPdfFileAsync(orders, template, "orders.pdf");
```

#### XML

```csharp
var products = await GetProductsAsync();
await xmlService.ExportDynamic("products.xml", products);
```

#### Azure Translation

```csharp
var translator = new TranslatorService(apiKey, endpoint, documentEndpoint);
string translated = await translator.DirectTranslateAsync("Hello, world!", "es");
// Result: "¡Hola, mundo!"
```

#### Azure Vision

```csharp
var result = await visionService.AnalyzeImageAsync("https://example.com/image.jpg");
Console.WriteLine($"Description: {result.Caption}");
Console.WriteLine($"Text found: {string.Join(", ", result.ExtractedText)}");
```

## Dependency Injection Setup

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Email
    services.AddSingleton<IEmailService>(sp => new MailKitEmailService(
        Configuration["Email:SmtpHost"],
        Configuration.GetValue<int>("Email:SmtpPort"),
        Configuration["Email:FromName"],
        Configuration["Email:FromAddress"],
        Configuration["Email:Password"],
        Environment.IsDevelopment(),
        Configuration.GetSection("Email:Developers").Get<string[]>()
    ));

    services.AddSingleton(sp => new EmailTemplateService(
        TemplateConfiguration.WithTemplateDataTypes(typeof(WelcomeEmailTemplateData))
    ));

    // PDF
    services.AddScoped<IPdfExportService, PdfExportService>();
    services.AddScoped<IPdfTemplateService, PdfTemplateService>();
    services.AddScoped<IPdfOptimizationService, PdfOptimizationService>();
    services.AddScoped<IPdfService, PdfTextService>();

    // XML
    services.AddScoped<IXmlService, XmlService>();

    // Azure
    services.AddSingleton(sp => new TranslatorService(
        Configuration["Azure:Translator:Key"],
        Configuration["Azure:Translator:Endpoint"],
        Configuration["Azure:Translator:DocumentEndpoint"]
    ));

    services.Configure<VisionServiceOptions>(Configuration.GetSection("Azure:Vision"));
    services.AddScoped<IVisionService, AzureVisionService>();
}
```

## Common Integration Scenarios

### Email + Templates
Send beautifully formatted emails using Fluid templates:
```csharp
var result = await templateService.RenderEmailAsync(welcomeData);
await emailService.SendEmailAsync(user.Email, result.Subject, result.HtmlBody);
```

### Email + PDF
Send reports as PDF attachments:
```csharp
var pdfBytes = await pdfExportService.ExportToPdfAsync(reportData, template);
await emailService.SendEmailAsync(user.Email, "Report", body,
    attachments: new[] { ("Report.pdf", pdfBytes) });
```

### Translation + Email
Send multi-language emails:
```csharp
var translatedSubject = await translator.DirectTranslateAsync("Welcome!", user.Language);
var translatedBody = await translator.DirectTranslateAsync(body, user.Language);
await emailService.SendEmailAsync(user.Email, translatedSubject, translatedBody);
```

### Vision + Database
Catalog images automatically:
```csharp
var analysis = await visionService.AnalyzeImageAsync(imageUrl);
await db.Images.UpdateAsync(imageId, new {
    Description = analysis.Caption,
    Tags = analysis.Tags.Select(t => t.Name).ToList()
});
```

## Package Dependencies

Key NuGet packages used:

- **Geocoding**: GoogleApi (4.6.0) for Google Maps integration
- **Email**: MailKit (4.14.1), Fluid.Core (2.30.0)
- **PDF**: itext (9.3.0), itext.pdfoptimizer (4.1.0), ILove_PDF (1.6.2)
- **XML**: Built-in System.Xml
- **Azure**: Azure.AI.Translation.Document (2.0.0), Azure.AI.Vision.ImageAnalysis (1.0.0)
- **Data Exchange**: ClosedXML (0.105.0), CsvHelper (33.1.0)
- **Misc**: ImageSharp (3.1.12), ZXing.Net (0.16.11), Twilio (7.13.5)

## Configuration Examples

### appsettings.json

```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "FromName": "My Application",
    "FromAddress": "noreply@myapp.com",
    "Password": "app-password",
    "Developers": ["dev1@myapp.com", "dev2@myapp.com"]
  },
  "Azure": {
    "Translator": {
      "Key": "your-translator-key",
      "Endpoint": "https://api.cognitive.microsofttranslator.com",
      "DocumentEndpoint": "https://your-resource.cognitiveservices.azure.com"
    },
    "Vision": {
      "Endpoint": "https://your-vision.cognitiveservices.azure.com",
      "ApiKey": "your-vision-key",
      "DefaultLanguage": "en",
      "GenderNeutralCaption": true
    }
  }
}
```

## Best Practices

1. **Email**: Use development mode during testing to avoid sending real emails
2. **PDF**: Choose appropriate optimization levels (Balanced for most cases)
3. **XML**: Use strongly-typed serialization for configuration files
4. **Azure**: Implement caching for translation to reduce API calls
5. **General**: Always use async methods for better scalability
6. **Security**: Store API keys in Azure Key Vault or User Secrets
7. **Error Handling**: Check result objects (IsValid, IsSuccess) before proceeding
8. **Resource Management**: Dispose services properly (TranslatorService implements IDisposable)

## Performance Tips

- **Email**: Batch operations for multiple recipients
- **PDF**: Use file-based operations for large datasets
- **XML**: Work with files instead of strings for large data
- **Azure Translation**: Use batch translation for multiple texts (up to 100)
- **Azure Vision**: Optimize image sizes before analysis
- **General**: Use dependency injection for proper lifecycle management

## Support & Documentation

- **GitHub**: [CheapNud/CheapHelpers](https://github.com/CheapNud/CheapHelpers)
- **NuGet**: [CheapHelpers.Services](https://www.nuget.org/packages/CheapHelpers.Services)
- **License**: MIT

## Related Packages

- **CheapHelpers** - Core utilities and extensions
- **CheapHelpers.Models** - Shared models and DTOs
- **CheapHelpers.EF** - Entity Framework utilities
- **CheapHelpers.Blazor** - Blazor components

## Detailed Documentation

For in-depth guides, examples, and API reference:

- [Geocoding Services Documentation](Geocoding.md)
- [Email Services Documentation](Email.md)
- [PDF Services Documentation](PDF.md)
- [XML Services Documentation](XML.md)
- [Azure Services Documentation](Azure.md)
