# CheapHelpers.Services Documentation

Business services including email with templates, PDF generation, Azure integration, and document processing.

## Package Information

- **Package**: CheapHelpers.Services
- **Target Framework**: .NET 11.0 (net11.0)
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

### Voice Services
Speech-to-text behind `ISpeechToText` and text-to-speech behind `ITextToSpeech`. Providers never throw for network or auth errors: they log and return an empty result. Cancellation still propagates.

| Call | Registers | Config section |
|---|---|---|
| `AddWhisperSpeechToText()` | `WhisperSpeechToText` (typed HttpClient), `ISpeechToText` if not yet registered | `Voice:Whisper`: `BaseUrl`, `ApiKey`, `Model`, `Language`, `Timeout` |
| `AddAzureSpeechToText()` | `AzureSpeechToText` (typed HttpClient), `ISpeechToText` if not yet registered | `Voice:Azure`: `Region`, `SubscriptionKey`, `Language` |
| `AddEdgeTextToSpeech()` | `EdgeTextToSpeech` and `EdgeTtsVoiceCatalog` singletons, `ITextToSpeech` and `TimeProvider.System` if not yet registered | `Voice:Edge`: `Voice`, `Rate`, `Pitch`, `Volume` |
| `AddAzureTextToSpeech()` | `AzureTextToSpeech` (typed HttpClient), `ITextToSpeech` if not yet registered | `Voice:Azure`: `Region`, `SubscriptionKey`, `Language`, `Voice`, `OutputFormat` |

The first provider added becomes the interface default; inject the concrete type to pick another. Whisper works against any OpenAI-compatible server (set `BaseUrl` to a local whisper.cpp). Azure speech-to-text takes 16 kHz mono PCM WAV of at most 60 seconds. Edge text-to-speech is the free Edge read-aloud service: no key, MP3 output, and `EdgeTtsVoiceCatalog.GetVoicesAsync()` lists its voices. Each call takes optional `SttOptions` (language, model) or `TtsOptions` (voice, language).

```csharp
builder.Services.Configure<WhisperSttOptions>(builder.Configuration.GetSection(WhisperSttOptions.SectionName));
builder.Services.AddWhisperSpeechToText();
```

### Image Helpers
`ImageHelpers.EncodePng` turns a raw 8-bit pixel buffer (RGB, BGR, RGBA or BGRA) into a PNG, optionally shrunk so its longest side fits a cap. `ImageHelpers.NormalizePhoto` turns an uploaded photo upright and re-encodes it as JPEG without EXIF or other metadata, and returns null for formats libvips cannot load, such as BMP, so the caller can store the original. `ImageHelpers.DrawingToPng` turns a scanned drawing such as a 1-bit TIFF into an 8-bit greyscale PNG no wider than a given width, shrinking with a real kernel so thin lines survive; it never enlarges.

**Key Classes:**
- `ImageHelpers`, `PixelBandOrder`

```csharp
using CheapHelpers.Services.Imaging;

// A BGRA frame from a capture API, at most 1024 pixels on its longest side
byte[] png = ImageHelpers.EncodePng(frame, width, height, PixelBandOrder.Bgra, maxLongestSide: 1024);

// An uploaded photo; null means libvips cannot load it, so keep the original
byte[] jpeg = ImageHelpers.NormalizePhoto(uploadBytes) ?? uploadBytes;

// A scanned 1-bit TIFF drawing as a greyscale PNG at most 1600 pixels wide
byte[] drawing = ImageHelpers.DrawingToPng(tiffBytes, maxWidth: 1600);
```

For bulk runs over many drawings, set `NetVips.Cache.Max = 0` once at startup so the operation cache does not keep their buffers alive.

### Networking
The network scanner sweeps subnets, resolves hostnames and MAC addresses, and finds devices through UPnP/SSDP and mDNS. Pluggable detectors then identify each device type over HTTP, SSH and Windows services.

- [Scanner](Networking/Scanner.md) - NetworkScanner usage and configuration
- [Detectors](Networking/Detectors.md) - Device type detection strategies
- [MACResolution](Networking/MACResolution.md) - Cross-platform MAC address resolution

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

    services.AddAzureVision(Configuration);
}
```

`AddAzureVision` binds the `AzureVision` section and validates the options at startup, so a missing `Endpoint` or `ApiKey` stops the app from starting instead of failing on the first analysis.

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

- **Geocoding**: GoogleApi (5.8.18) for Google Maps integration
- **Email**: MailKit (4.18.1), Fluid.Core (2.40.0), HtmlSanitizer (9.2.1039), SendGrid (9.29.3)
- **PDF**: itext (9.8.0), itext.pdfoptimizer (4.1.5), ILove_PDF (1.6.4)
- **XML**: Built-in System.Xml
- **Azure**: Azure.AI.Translation.Document (3.0.0), Azure.AI.Vision.ImageAnalysis (1.0.0), Azure.Identity (1.21.0), Azure.Storage.Blobs (12.30.1)
- **Data Exchange**: ClosedXML (0.105.1), CsvHelper (33.1.0), UblSharp (1.1.1)
- **Misc**: NetVips (3.2.0) with NetVips.Native (8.18.7), Twilio (8.1.0)
- **Messaging and real-time**: RabbitMQ.Client (7.2.2), Microsoft.AspNetCore.SignalR.Client (10.0.14), Microsoft.AspNetCore.Http.Connections (1.2.13)
- **Identity and Graph**: Duende.IdentityModel (8.1.0), Microsoft.Graph (6.8.0)
- **Caching**: Microsoft.Extensions.Caching.Memory (10.0.14)

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
    }
  },
  "AzureVision": {
    "Endpoint": "https://your-vision.cognitiveservices.azure.com",
    "ApiKey": "your-vision-key",
    "DefaultLanguage": "en",
    "GenderNeutralCaption": true
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

- **Repository**: [CheapNud/CheapHelpers](https://github.com/CheapNud/CheapHelpers)
- **NuGet**: [CheapHelpers.Services](https://www.nuget.org/packages/CheapHelpers.Services)
- **License**: MIT

## Related Packages

- **CheapHelpers** - Core utilities and extensions
- **CheapHelpers.EF** - Entity Framework utilities
- **CheapHelpers.Blazor** - Blazor components
- **CheapHelpers.Barcodes** - Barcode generation and reading; Services adds the `ReadBarcode(byte[])` extension that decodes image files
- **CheapHelpers.Auth** - Plex, OAuth, OpenID Connect and API keys moved there in 4.0.0: see [Auth](../Auth/README.md)

## Detailed Documentation

For in-depth guides, examples, and API reference:

- [Geocoding Services Documentation](Geocoding.md)
- [Email Services Documentation](Email.md)
- [PDF Services Documentation](PDF.md)
- [XML Services Documentation](XML.md)
- [Azure Services Documentation](Azure.md)
