# Azure Services

Azure Cognitive Services integration for Translation, Vision, and Document processing.

## Table of Contents

- [Overview](#overview)
- [Available Services](#available-services)
- [Configuration](#configuration)
- [Translation Service](#translation-service)
- [Vision Service](#vision-service)
- [Dependency Injection Setup](#dependency-injection-setup)
- [Common Scenarios](#common-scenarios)

## Overview

The CheapHelpers.Services Azure package provides integration with Azure Cognitive Services:

1. **TranslatorService** - Azure Translator API for text and document translation
2. **AzureVisionService** - Azure Computer Vision for image analysis

### Key Features

**Translation:**
- Text translation with 19+ supported languages
- Batch translation for multiple texts
- Language detection
- Document translation support
- Built-in caching for performance
- Automatic retry logic

**Vision:**
- Image analysis (captions, tags, objects)
- OCR (text extraction from images)
- Object detection with bounding boxes
- Confidence scoring
- URL and stream-based input

## Available Services

### TranslatorService

```csharp
public class TranslatorService : IDisposable
{
    public TranslatorService(
        string apiKey,
        string endpoint,
        string documentEndpoint,
        HttpClient? httpClient = null,
        ILogger<TranslatorService>? logger = null
    );

    // Simple translation
    Task<string?> DirectTranslateAsync(string textToTranslate, string to = "en",
        string? from = null, CancellationToken cancellationToken = default);

    // Detailed translation
    Task<List<AzureTranslation>> TranslateAsync(string textToTranslate, string to = "en",
        string? from = null, CancellationToken cancellationToken = default);

    // Batch translation
    Task<List<AzureTranslation>> TranslateBatchAsync(IEnumerable<string> textsToTranslate,
        string to = "en", string? from = null, CancellationToken cancellationToken = default);

    // Language detection
    Task<string?> DetectLanguageAsync(string text, CancellationToken cancellationToken = default);

    // Document translation
    Task TranslateDocumentAsync(string sourceUrl, string destinationUrl,
        string? targetLanguage = null, CancellationToken cancellationToken = default);
}
```

### IVisionService / AzureVisionService

```csharp
public interface IVisionService
{
    Task<VisionAnalysisResult> AnalyzeImageAsync(string imageUrl,
        CancellationToken cancellationToken = default);
    Task<VisionAnalysisResult> AnalyzeImageAsync(Stream imageStream,
        CancellationToken cancellationToken = default);
}
```

## Configuration

### Translation Service Configuration

```csharp
var translatorService = new TranslatorService(
    apiKey: "your-azure-translator-key",
    endpoint: "https://api.cognitive.microsofttranslator.com",
    documentEndpoint: "https://your-resource.cognitiveservices.azure.com",
    httpClient: null,  // Optional: provide your own HttpClient
    logger: null       // Optional: ILogger for diagnostics
);
```

### Vision Service Configuration

```csharp
// Via Options Pattern
services.Configure<VisionServiceOptions>(options =>
{
    options.Endpoint = "https://your-resource.cognitiveservices.azure.com";
    options.ApiKey = "your-vision-api-key";
    options.DefaultLanguage = "en";
    options.GenderNeutralCaption = true;
});

services.AddScoped<IVisionService, AzureVisionService>();
```

**VisionServiceOptions:**
```csharp
public class VisionServiceOptions
{
    public string Endpoint { get; set; }
    public string ApiKey { get; set; }
    public string DefaultLanguage { get; set; } = "en";
    public bool GenderNeutralCaption { get; set; } = true;
}
```

### Azure Portal Setup

1. **Create Translator Resource:**
   - Go to Azure Portal
   - Create "Translator" resource
   - Copy Key and Endpoint
   - Note the Document Translation endpoint

2. **Create Computer Vision Resource:**
   - Create "Computer Vision" resource
   - Copy Key and Endpoint
   - Enable features you need

## Translation Service

### Supported Languages

The service supports 19 major languages:

```
en (English)      nl (Dutch)        fr (French)       de (German)
es (Spanish)      it (Italian)      pt (Portuguese)   ru (Russian)
zh (Chinese)      ja (Japanese)     ko (Korean)       ar (Arabic)
hi (Hindi)        tr (Turkish)      pl (Polish)       sv (Swedish)
da (Danish)       no (Norwegian)    fi (Finnish)
```

### Basic Translation

```csharp
// Simple translation - returns just the text
string translated = await translatorService.DirectTranslateAsync(
    textToTranslate: "Hello, world!",
    to: "es"  // Translate to Spanish
);
// Result: "¡Hola, mundo!"

// With source language specified
string translated = await translatorService.DirectTranslateAsync(
    textToTranslate: "Hello, world!",
    to: "fr",
    from: "en"
);
// Result: "Bonjour le monde!"
```

### Detailed Translation

```csharp
// Get full translation details
var results = await translatorService.TranslateAsync(
    textToTranslate: "The weather is nice today.",
    to: "de"
);

foreach (var result in results)
{
    foreach (var translation in result.Translations)
    {
        Console.WriteLine($"Text: {translation.Text}");
        Console.WriteLine($"To: {translation.To}");
    }

    if (result.DetectedLanguage != null)
    {
        Console.WriteLine($"Detected: {result.DetectedLanguage.Language}");
        Console.WriteLine($"Score: {result.DetectedLanguage.Score}");
    }
}
```

**AzureTranslation Model:**
```csharp
public class AzureTranslation
{
    public DetectedLanguage? DetectedLanguage { get; set; }
    public List<TranslationResult> Translations { get; set; }
}

public class TranslationResult
{
    public string Text { get; set; }
    public string To { get; set; }
}

public class DetectedLanguage
{
    public string Language { get; set; }
    public float Score { get; set; }
}
```

### Batch Translation

```csharp
// Translate multiple texts in one call (up to 100)
var texts = new[]
{
    "Hello, world!",
    "How are you?",
    "Thank you very much."
};

var results = await translatorService.TranslateBatchAsync(
    textsToTranslate: texts,
    to: "ja"  // Translate all to Japanese
);

for (int i = 0; i < texts.Length; i++)
{
    var originalText = texts[i];
    var translatedText = results[i].Translations.First().Text;
    Console.WriteLine($"{originalText} → {translatedText}");
}
```

### Language Detection

```csharp
string detectedLanguage = await translatorService.DetectLanguageAsync(
    text: "Bonjour, comment allez-vous?"
);
// Result: "fr"

// Use detected language for translation
var text = "Ciao, come stai?";
var language = await translatorService.DetectLanguageAsync(text);
var translated = await translatorService.DirectTranslateAsync(text, to: "en", from: language);
```

### Document Translation

```csharp
// Translate entire documents (PDF, DOCX, etc.)
await translatorService.TranslateDocumentAsync(
    sourceUrl: "https://yourstorage.blob.core.windows.net/input/document.pdf",
    destinationUrl: "https://yourstorage.blob.core.windows.net/output/document-es.pdf",
    targetLanguage: "es"
);

// Uses current culture if target language not specified
await translatorService.TranslateDocumentAsync(
    sourceUrl: sourceUrl,
    destinationUrl: destUrl
);
```

### Translation Caching

The service automatically caches translation results:

```csharp
// First call - hits the API
var result1 = await translatorService.TranslateAsync("Hello", "es");

// Second call with same text - returns from cache (60 min default)
var result2 = await translatorService.TranslateAsync("Hello", "es");

// Cache key is based on: text + source language + target language
```

### Retry Logic

Built-in retry with exponential backoff:

```csharp
// Automatically retries up to 3 times on HTTP errors
// Delays: 1000ms, 2000ms, 3000ms
var result = await translatorService.TranslateAsync("Text to translate", "fr");
```

## Vision Service

### Image Analysis Results

```csharp
public class VisionAnalysisResult
{
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; }
    public string Caption { get; set; }              // Description of image
    public double CaptionConfidence { get; set; }    // 0.0 to 1.0
    public List<string> ExtractedText { get; set; }  // OCR text
    public List<VisionTag> Tags { get; set; }        // Image tags
    public List<VisionDetectedObject> Objects { get; set; }  // Detected objects
}

public class VisionTag
{
    public string Name { get; set; }
    public double Confidence { get; set; }
}

public class VisionDetectedObject
{
    public string Name { get; set; }
    public double Confidence { get; set; }
    public VisionBoundingBox BoundingBox { get; set; }
}

public class VisionBoundingBox
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
```

### Analyze Image from URL

```csharp
var result = await visionService.AnalyzeImageAsync(
    imageUrl: "https://example.com/image.jpg"
);

if (result.IsSuccess)
{
    Console.WriteLine($"Caption: {result.Caption}");
    Console.WriteLine($"Confidence: {result.CaptionConfidence:P0}");

    Console.WriteLine("\nExtracted Text:");
    foreach (var text in result.ExtractedText)
    {
        Console.WriteLine($"  {text}");
    }

    Console.WriteLine("\nTags:");
    foreach (var tag in result.Tags)
    {
        Console.WriteLine($"  {tag.Name} ({tag.Confidence:P0})");
    }

    Console.WriteLine("\nDetected Objects:");
    foreach (var obj in result.Objects)
    {
        Console.WriteLine($"  {obj.Name} at ({obj.BoundingBox.X}, {obj.BoundingBox.Y})");
    }
}
else
{
    Console.WriteLine($"Analysis failed: {result.ErrorMessage}");
}
```

### Analyze Image from Stream

```csharp
using var imageStream = File.OpenRead("photo.jpg");

var result = await visionService.AnalyzeImageAsync(imageStream);

if (result.IsSuccess)
{
    Console.WriteLine($"Image description: {result.Caption}");
}
```

### OCR Text Extraction

```csharp
var result = await visionService.AnalyzeImageAsync("receipt.jpg");

if (result.IsSuccess && result.ExtractedText.Any())
{
    Console.WriteLine("Text found in image:");
    foreach (var line in result.ExtractedText)
    {
        Console.WriteLine(line);
    }

    // Process extracted text
    var receiptText = string.Join(" ", result.ExtractedText);
    var totalMatch = Regex.Match(receiptText, @"Total:\s*\$?(\d+\.?\d*)");
    if (totalMatch.Success)
    {
        Console.WriteLine($"Found total: ${totalMatch.Groups[1].Value}");
    }
}
```

### Object Detection

```csharp
var result = await visionService.AnalyzeImageAsync(imageUrl);

// Find all people in the image
var people = result.Objects
    .Where(o => o.Name.ToLower() == "person" && o.Confidence > 0.7)
    .ToList();

Console.WriteLine($"Found {people.Count} people in the image");

foreach (var person in people)
{
    var bbox = person.BoundingBox;
    Console.WriteLine($"Person at position ({bbox.X}, {bbox.Y}), " +
                     $"size {bbox.Width}x{bbox.Height}");
}
```

## Dependency Injection Setup

### Complete Registration

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Translation Service
    services.AddSingleton(sp =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        return new TranslatorService(
            apiKey: config["Azure:Translator:Key"],
            endpoint: config["Azure:Translator:Endpoint"],
            documentEndpoint: config["Azure:Translator:DocumentEndpoint"],
            httpClient: sp.GetService<IHttpClientFactory>()?.CreateClient(),
            logger: sp.GetService<ILogger<TranslatorService>>()
        );
    });

    // Vision Service
    services.Configure<VisionServiceOptions>(
        Configuration.GetSection("Azure:Vision")
    );
    services.AddScoped<IVisionService, AzureVisionService>();
}
```

### appsettings.json

```json
{
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

### User Secrets (Development)

```bash
dotnet user-secrets set "Azure:Translator:Key" "your-translator-key"
dotnet user-secrets set "Azure:Vision:ApiKey" "your-vision-key"
```

## Common Scenarios

### Scenario 1: Multi-Language Content Platform

```csharp
public class ContentService
{
    private readonly TranslatorService _translator;
    private readonly IContentRepository _contentRepo;

    public async Task PublishMultiLanguageAsync(Content content, string[] targetLanguages)
    {
        // Save original content
        await _contentRepo.SaveAsync(content);

        // Translate to all target languages
        foreach (var language in targetLanguages)
        {
            var translatedTitle = await _translator.DirectTranslateAsync(
                content.Title, to: language, from: "en"
            );

            var translatedBody = await _translator.DirectTranslateAsync(
                content.Body, to: language, from: "en"
            );

            var translatedContent = new Content
            {
                Title = translatedTitle,
                Body = translatedBody,
                Language = language,
                OriginalId = content.Id
            };

            await _contentRepo.SaveAsync(translatedContent);
        }
    }
}
```

### Scenario 2: Product Description Translation

```csharp
public class ProductTranslationService
{
    private readonly TranslatorService _translator;

    public async Task<Product> TranslateProductAsync(Product product, string targetLanguage)
    {
        var textsToTranslate = new[]
        {
            product.Name,
            product.Description,
            product.ShortDescription
        };

        var results = await _translator.TranslateBatchAsync(
            textsToTranslate,
            to: targetLanguage
        );

        return new Product
        {
            Id = product.Id,
            Name = results[0].Translations.First().Text,
            Description = results[1].Translations.First().Text,
            ShortDescription = results[2].Translations.First().Text,
            Language = targetLanguage
        };
    }
}
```

### Scenario 3: Auto-Detect and Translate User Input

```csharp
public class ChatService
{
    private readonly TranslatorService _translator;

    public async Task<string> TranslateUserMessageAsync(string message, string targetLanguage)
    {
        // Detect source language
        var sourceLanguage = await _translator.DetectLanguageAsync(message);

        // Skip if already in target language
        if (sourceLanguage == targetLanguage)
            return message;

        // Translate
        return await _translator.DirectTranslateAsync(
            message,
            to: targetLanguage,
            from: sourceLanguage
        );
    }
}
```

### Scenario 4: Document Translation Pipeline

```csharp
public class DocumentTranslationPipeline
{
    private readonly TranslatorService _translator;
    private readonly IBlobStorage _storage;

    public async Task TranslateDocumentAsync(string documentId, string[] targetLanguages)
    {
        var sourceUrl = await _storage.GetDocumentUrlAsync(documentId);

        foreach (var language in targetLanguages)
        {
            var destinationUrl = await _storage.CreateDestinationUrlAsync(
                documentId,
                language
            );

            await _translator.TranslateDocumentAsync(
                sourceUrl,
                destinationUrl,
                targetLanguage: language
            );

            // Update database
            await UpdateTranslationStatusAsync(documentId, language, "Completed");
        }
    }
}
```

### Scenario 5: Image Moderation with Vision

```csharp
public class ImageModerationService
{
    private readonly IVisionService _visionService;

    public async Task<bool> IsImageAppropriateAsync(Stream imageStream)
    {
        var result = await _visionService.AnalyzeImageAsync(imageStream);

        if (!result.IsSuccess)
            return false; // Fail-safe: reject if analysis fails

        // Check tags for inappropriate content
        var inappropriateTags = new[] { "adult", "violence", "gore", "weapon" };

        foreach (var tag in result.Tags)
        {
            if (inappropriateTags.Contains(tag.Name.ToLower()) && tag.Confidence > 0.7)
            {
                return false;
            }
        }

        return true;
    }
}
```

### Scenario 6: Receipt Processing

```csharp
public class ReceiptProcessingService
{
    private readonly IVisionService _visionService;

    public async Task<Receipt> ProcessReceiptImageAsync(Stream receiptImage)
    {
        var result = await _visionService.AnalyzeImageAsync(receiptImage);

        if (!result.IsSuccess || !result.ExtractedText.Any())
        {
            throw new InvalidOperationException("Could not extract text from receipt");
        }

        var receiptText = string.Join("\n", result.ExtractedText);

        return new Receipt
        {
            RawText = receiptText,
            Total = ExtractTotal(receiptText),
            Date = ExtractDate(receiptText),
            Vendor = ExtractVendor(receiptText),
            Items = ExtractLineItems(receiptText)
        };
    }

    private decimal ExtractTotal(string text)
    {
        var match = Regex.Match(text, @"Total:?\s*\$?(\d+\.?\d*)");
        if (match.Success && decimal.TryParse(match.Groups[1].Value, out var total))
            return total;
        return 0;
    }

    // Additional extraction methods...
}
```

### Scenario 7: Image Cataloging

```csharp
public class ImageCatalogService
{
    private readonly IVisionService _visionService;
    private readonly IImageRepository _imageRepo;

    public async Task CatalogImageAsync(string imageUrl, int imageId)
    {
        var result = await _visionService.AnalyzeImageAsync(imageUrl);

        if (result.IsSuccess)
        {
            var metadata = new ImageMetadata
            {
                ImageId = imageId,
                Description = result.Caption,
                Tags = result.Tags.Select(t => t.Name).ToList(),
                DetectedObjects = result.Objects.Select(o => o.Name).Distinct().ToList(),
                ContainsText = result.ExtractedText.Any(),
                AnalyzedAt = DateTime.UtcNow
            };

            await _imageRepo.SaveMetadataAsync(metadata);
        }
    }
}
```

### Scenario 8: Multi-Language Email with Translation

```csharp
public class InternationalEmailService
{
    private readonly IEmailService _emailService;
    private readonly TranslatorService _translator;

    public async Task SendWelcomeEmailAsync(User user)
    {
        var userLanguage = user.PreferredLanguage ?? "en";

        // Translate email content
        var subject = await _translator.DirectTranslateAsync(
            "Welcome to our service!",
            to: userLanguage,
            from: "en"
        );

        var body = await _translator.DirectTranslateAsync(
            $"Hello {user.Name}, thank you for joining us!",
            to: userLanguage,
            from: "en"
        );

        await _emailService.SendEmailAsync(
            recipient: user.Email,
            subject: subject,
            body: body
        );
    }
}
```

### Scenario 9: Product Search by Image

```csharp
public class VisualSearchService
{
    private readonly IVisionService _visionService;
    private readonly IProductRepository _productRepo;

    public async Task<List<Product>> FindSimilarProductsAsync(Stream queryImage)
    {
        var result = await _visionService.AnalyzeImageAsync(queryImage);

        if (!result.IsSuccess)
            return new List<Product>();

        // Extract key tags
        var searchTags = result.Tags
            .Where(t => t.Confidence > 0.6)
            .OrderByDescending(t => t.Confidence)
            .Take(5)
            .Select(t => t.Name)
            .ToList();

        // Search products by tags
        return await _productRepo.FindByTagsAsync(searchTags);
    }
}
```

## Performance Considerations

### Translation Caching

- **Default cache duration**: 60 minutes
- **Cache key**: SHA256 hash of text + source + target languages
- Caching reduces API calls and improves response time

### Batch Operations

```csharp
// Good - Single API call for multiple texts (up to 100)
await translator.TranslateBatchAsync(texts, "fr");

// Bad - Multiple API calls
foreach (var text in texts)
{
    await translator.DirectTranslateAsync(text, "fr");
}
```

### HttpClient Management

```csharp
// Provide shared HttpClient for better connection pooling
var httpClient = httpClientFactory.CreateClient();
var translator = new TranslatorService(apiKey, endpoint, docEndpoint, httpClient);
```

### Vision Service Optimization

- Use appropriate image sizes (not too large)
- Stream images when possible to reduce memory
- Consider confidence thresholds when processing results

## Error Handling

### Translation Errors

```csharp
try
{
    var result = await translator.TranslateAsync("Text", "fr");
}
catch (HttpRequestException ex)
{
    // Network or API errors
    _logger.LogError(ex, "Translation API request failed");
}
catch (ArgumentException ex)
{
    // Invalid language code or parameters
    _logger.LogError(ex, "Invalid translation parameters");
}
```

### Vision Errors

```csharp
var result = await visionService.AnalyzeImageAsync(imageUrl);

if (!result.IsSuccess)
{
    _logger.LogWarning($"Vision analysis failed: {result.ErrorMessage}");
    // Handle error gracefully
}
```

## Best Practices

1. **Store API keys securely**: Use Azure Key Vault or User Secrets
2. **Implement caching**: Translation service has built-in caching
3. **Use batch operations**: More efficient than individual calls
4. **Handle language fallbacks**: Default to English if translation fails
5. **Validate language codes**: Service validates against supported languages
6. **Use ILogger**: Enable logging for diagnostics
7. **Dispose properly**: TranslatorService implements IDisposable
8. **Set confidence thresholds**: Filter vision results by confidence (e.g., > 0.7)
9. **Monitor usage**: Track API calls for cost management
10. **Implement retry logic**: Built-in for translation, implement for vision if needed

## Cost Management

### Translation Pricing

- Character-based pricing
- Use batch operations to reduce overhead
- Cache frequently translated content
- Monitor monthly usage

### Vision Pricing

- Per-transaction pricing
- Different rates for different features
- Consider analyzing only when necessary
- Cache analysis results when appropriate

## Related Documentation

- [Email Services](Email.md) - Send translated emails
- [PDF Services](PDF.md) - Translate PDF documents
- [XML Services](XML.md) - Data exchange for translated content
