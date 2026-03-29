# CheapHelpers

A collection of production-ready C# utilities, extensions, and services for .NET 11.0 development. Simplify common development tasks with battle-tested helpers for Blazor, Entity Framework, networking, email, PDF generation, billing, reporting, and more.

## Packages

| Package | Version | Description |
|---------|---------|-------------|
| [CheapHelpers](https://www.nuget.org/packages/CheapHelpers) | ![NuGet](https://img.shields.io/nuget/v/CheapHelpers.svg) | Core utilities, extensions, threading, and scheduling |
| [CheapHelpers.Models](https://www.nuget.org/packages/CheapHelpers.Models) | ![NuGet](https://img.shields.io/nuget/v/CheapHelpers.Models.svg) | Shared entities, DTOs, enums, and UBL invoice models |
| [CheapHelpers.EF](https://www.nuget.org/packages/CheapHelpers.EF) | ![NuGet](https://img.shields.io/nuget/v/CheapHelpers.EF.svg) | Entity Framework repository pattern with Identity support |
| [CheapHelpers.Services](https://www.nuget.org/packages/CheapHelpers.Services) | ![NuGet](https://img.shields.io/nuget/v/CheapHelpers.Services.svg) | Email, PDF, billing, reporting, notifications, auth, and integrations |
| [CheapHelpers.Blazor](https://www.nuget.org/packages/CheapHelpers.Blazor) | ![NuGet](https://img.shields.io/nuget/v/CheapHelpers.Blazor.svg) | Blazor components, account module, auth endpoints, and Hybrid features |
| [CheapHelpers.Networking](https://www.nuget.org/packages/CheapHelpers.Networking) | ![NuGet](https://img.shields.io/nuget/v/CheapHelpers.Networking.svg) | Network scanning, mDNS discovery, and device detection |
| [CheapHelpers.MAUI](https://www.nuget.org/packages/CheapHelpers.MAUI) | ![NuGet](https://img.shields.io/nuget/v/CheapHelpers.MAUI.svg) | MAUI platform implementations (iOS APNS, Android FCM) |
| [CheapHelpers.MediaProcessing](https://www.nuget.org/packages/CheapHelpers.MediaProcessing) | ![NuGet](https://img.shields.io/nuget/v/CheapHelpers.MediaProcessing.svg) | FFmpeg integration, hardware detection (Windows/Linux) |
| [CheapHelpers.Avalonia.Bridge](https://www.nuget.org/packages/CheapHelpers.Avalonia.Bridge) | ![NuGet](https://img.shields.io/nuget/v/CheapHelpers.Avalonia.Bridge.svg) | Desktop notification bridge for Avalonia apps |

## Installation

```bash
# Core utilities and extensions
dotnet add package CheapHelpers

# Entity Framework repository pattern
dotnet add package CheapHelpers.EF

# Business services (email, PDF, billing, reporting, notifications)
dotnet add package CheapHelpers.Services

# Blazor components, account module, and Hybrid features
dotnet add package CheapHelpers.Blazor

# Network scanning and device discovery
dotnet add package CheapHelpers.Networking
```

## Quick Start

### String Extensions
```csharp
using CheapHelpers.Extensions;

"hello world".Capitalize();                    // "Hello world"
"0474123456".ToInternationalPhoneNumber();     // "+32474123456"
"Very long text here".TrimWithEllipsis(10);    // "Very long ..."
"my:invalid*file".SanitizeFileName();          // "my_invalid_file"
```

### Email Service (3 providers)
```csharp
using CheapHelpers.Services.Email;

// MailKit (SMTP), Microsoft Graph, or SendGrid
var emailService = new SendGridEmailService(
    fromName: "MyApp",
    fromAddress: "noreply@myapp.com",
    apiKey: "SG.xxx",
    inDev: false,
    developers: ["dev@myapp.com"]);

await emailService.SendEmailAsync(
    "user@example.com", "Welcome!", "<h1>Welcome!</h1>",
    attachments: [("invoice.pdf", pdfBytes)]);
```

### Email Templates (Fluid/Liquid)
```csharp
// Template data class
public class WelcomeEmailTemplateData : BaseEmailTemplateData
{
    public override string Subject => $"Welcome, {UserName}!";
    public string UserName { get; set; }
    public string ActivationLink { get; set; }
}

// Render and send
var templateService = new EmailTemplateService(...);
var result = await templateService.RenderEmailAsync(new WelcomeEmailTemplateData
{
    UserName = "John",
    ActivationLink = "https://myapp.com/activate/abc123"
});
await emailService.SendEmailAsync("john@example.com", result.Subject, result.HtmlBody);
```

### Notification System
```csharp
// Register with multi-channel support
services.AddCheapNotifications<MyUser>(options =>
{
    options.EnabledChannels = NotificationChannelFlags.InApp | NotificationChannelFlags.Email;
    options.RealTimeProvider = "SignalR"; // Default. Use "RabbitMQ" for multi-server
});

// Blazor real-time (SignalR)
services.AddCheapNotificationsBlazor();
app.MapCheapNotificationsHub();

// Optional: RabbitMQ for cross-server delivery (SignalR stays client-facing)
services.AddCheapNotificationsRabbitMQConsumer("amqp://localhost");
```

### API Key System
```csharp
services.AddCheapApiKeys<MyUser>(options =>
{
    options.KeyPrefix = "myapp_";
    options.DefaultRateLimitPerMinute = 100;
});

// Generate — full key only returned once
var result = await apiKeyService.GenerateAsync(
    userId, "Production API",
    scopes: ["read", "write"],
    prefixOverride: "VTQ_",        // Per-call prefix override
    expiresAt: DateTime.UtcNow.AddYears(1));
// result.FullKey = "VTQ_a7f3bc91..." (store immediately!)

// Validate
var validation = await apiKeyService.ValidateAsync(rawKey);
if (validation.IsValid)
    Debug.WriteLine($"User: {validation.UserId}, Scopes: {string.Join(",", validation.Scopes)}");
```

### Billing Service (Metered Usage)
```csharp
services.AddCheapBilling<MyUser>(options =>
{
    options.DefaultTaxRate = 21.00m; // Belgian VAT
    options.InvoiceNumberPrefix = "INV";
});

// Record API usage (fire-and-forget safe)
await usageMeter.RecordUsageAsync(apiKeyId, "/api/data", "GET", 200, durationMs: 42);

// Generate invoice for a billing period
var invoice = await billingService.GenerateInvoiceAsync(
    apiKeyId, billingPlanId,
    new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));
```

### PEPPOL BIS 3.0 Invoicing
```csharp
using CheapHelpers.Services.DataExchange.Ubl;

var invoiceService = new UblInvoiceService();
var xml = await invoiceService.CreateInvoiceXmlAsync(new UblInvoice
{
    Id = "INV-2026-000001",
    IssueDate = DateTime.Now,
    DueDate = DateTime.Now.AddDays(30),
    Seller = new UblParty { Name = "My Company", EndpointId = "0123456789", TaxId = "BE0123456789" },
    Buyer = new UblParty { Name = "Customer", EndpointId = "9876543210" },
    Lines = [new UblInvoiceLine
    {
        Id = "1",
        Item = new UblItem { Name = "API calls - March 2026" },
        Quantity = 50000,
        UnitPrice = 0.001m,
        LineTotal = 50.00m,
        TaxCategory = new UblTaxCategory { Id = "S", Percent = 21.00m }
    }],
    TaxTotal = new UblTaxTotal { TaxAmount = 10.50m },
    Totals = new UblMonetaryTotals { PayableAmount = 60.50m }
});
// Generates PEPPOL BIS 3.0 compliant UBL 2.1 XML
```

### Reporting Service
```csharp
services.AddCheapReporting<MyUser>(options =>
{
    options.StorageProvider = StorageProviderType.AzureBlob;
    options.AzureBlobConnectionString = "...";
    options.DefaultRetentionDays = 90;
});

// Generate PDF report
var result = await reportService.GenerateReportAsync(new ReportRequest<SalesRecord>
{
    Name = "Monthly Sales",
    Format = ReportFormat.Pdf,
    Data = salesRecords,
    Template = myPdfTemplate,
    DistributeToEmails = ["manager@company.com"], // Auto-email after generation
    RetentionDays = 30
});

// Download later
var (content, fileName, mimeType) = await reportService.DownloadReportAsync(result.ReportId);
```

### Authentication (Multiple Providers)
```csharp
// Plex SSO
services.AddPlexAuth(options => { options.AdminToken = "..."; });
app.MapPlexAuthEndpoints(); // /auth/plex-start, /auth/plex-callback, /auth/logout

// Google + Microsoft OAuth
services.AddGoogleAuth(options => { options.ClientId = "..."; options.ClientSecret = "..."; });
services.AddMicrosoftAuth(options => { options.ClientId = "..."; options.ClientSecret = "..."; });

// GitHub + Apple OAuth
services.AddGitHubAuth(options => { options.ClientId = "..."; });
services.AddAppleAuth(options => { options.TeamId = "..."; options.KeyId = "..."; });

// Optional: auto-provision CheapUser from external auth
services.AddExternalUserProvisioning<MyUser>();
```

### Blazor Account Module
```csharp
// Register services
services.AddCheapHelpersBlazor<MyUser>(options =>
{
    options.UseEmailService<SendGridEmailService>();
    options.AccountRouteOptions = new AccountRouteOptions
    {
        LoginRoute = "/auth/login",
    };
});

// Consumer creates their own controller (one line):
[Route("Account/[action]")]
public class AccountController : CheapAccountController<MyUser>;

// Built-in pages: Login, Register, ForgotPassword, ResetPassword,
// ConfirmEmail, ConfirmEmailChange, SetPassword, ChangePassword,
// Authenticator (2FA), RecoveryCodes, PersonalData (GDPR), Lockout
// Account dashboard with extensible custom tabs
```

### Entity Framework
```csharp
// Generic context with Identity
services.AddCheapContext<MyUser>(options => options.UseSqlServer(connectionString))
    .AddIdentity<IdentityRole>();

// Generic repository with pagination
public class ProductRepo<TUser>(IDbContextFactory<CheapContext<TUser>> factory)
    : UserRepo<TUser>(factory) where TUser : CheapUser { }

var users = await userRepo.GetAllUsersPaginatedAsync(pageIndex: 1, pageSize: 20);
var stats = await userRepo.GetUserStatisticsAsync();
```

### Scheduling
```csharp
using CheapHelpers.Scheduling;

scheduledTaskService.RegisterDailyTask("cleanup", new TimeOnly(2, 0), async ct =>
{
    await notificationService.DeleteExpiredAsync(ct);
});

scheduledTaskService.RegisterMonthlyTask("billing", 1, new TimeOnly(3, 0), async ct =>
{
    await billingService.RunBillingCycleAsync(ct);
});
```

### Network Scanning
```csharp
services.AddNetworkScanning()
    .AddAllDetectors()  // UPnP, mDNS, HTTP, SSH
    .AddJsonStorage();

var scanner = serviceProvider.GetRequiredService<INetworkScanner>();
scanner.DeviceDiscovered += (device) =>
    Debug.WriteLine($"Found: {device.Name} ({device.IPv4Address})");
scanner.Start();
```

## Project Structure

```
CheapHelpers/                      Core utilities, extensions, scheduling, threading
CheapHelpers.Models/               Entities, DTOs, enums, UBL invoice models
CheapHelpers.EF/                   Entity Framework, CheapContext, repositories
CheapHelpers.Services/
  ├── ApiKeys/                     API key generation, validation, rate limiting
  ├── Auth/                        IExternalAuthProvider, Plex SSO service
  ├── Billing/                     Usage metering, invoice generation, billing cycles
  ├── DataExchange/
  │   ├── Csv/                     CSV import/export
  │   ├── Excel/                   Excel generation (ClosedXML)
  │   ├── Json/                    JSON file service
  │   ├── Pdf/                     PDF generation (iText) + optimization (iLovePDF)
  │   ├── Ubl/                     UBL Order/Invoice/CreditNote (UblSharp, PEPPOL BIS 3.0)
  │   └── Xml/                     XML serialization
  ├── Email/                       MailKit, Microsoft Graph, SendGrid + Fluid templates
  ├── Geocoding/                   Mapbox, Azure Maps, Google Maps, PTV Maps
  ├── Notifications/               Multi-channel (InApp, Email, SMS, Push) + RabbitMQ
  ├── Polling/                     HTTP polling with exponential backoff
  ├── Reporting/                   Report generation, storage, distribution, scheduling
  └── Storage/                     Azure Blob Storage
CheapHelpers.Blazor/
  ├── Components/                  NotificationBell, CultureSelector
  ├── Extensions/                  DI, Plex/OAuth/notification endpoint mapping
  ├── Helpers/                     AuthenticatorHelper, AccountTabDefinition
  ├── Hybrid/                      WebView bridge, push notifications, app bar
  ├── Middleware/                   API key middleware
  ├── Pages/Account/               Full account module (login, register, 2FA, GDPR)
  ├── Services/                    UserService, ExternalUserProvisioner, RabbitMQ consumer
  └── Shared/                      LoginDisplay, UploadFile, ImagePanel, Selectors
CheapHelpers.Networking/           Network scanner, mDNS discovery, device detectors
CheapHelpers.MediaProcessing/      FFmpeg integration, hardware detection (Win/Linux)
CheapHelpers.MAUI/                 iOS APNS, Android FCM, status bar helpers
CheapHelpers.Avalonia.Bridge/      Desktop notification adapter
```

## Key Dependencies

| Package | Used For |
|---------|----------|
| MailKit | SMTP email delivery |
| Microsoft.Graph | Microsoft Graph email |
| SendGrid | SendGrid email API |
| Fluid.Core | Liquid email templates |
| iText + iText.PdfOptimizer | PDF generation and optimization |
| ILove_PDF | Cloud PDF optimization |
| UblSharp | UBL 2.1 document generation (PEPPOL BIS 3.0) |
| RabbitMQ.Client | Cross-server notification transport |
| MudBlazor | Blazor UI components |
| ClosedXML | Excel file generation |
| Humanizer.Core | Relative timestamp formatting |
| FluentValidation | Form validation |
| Azure.Storage.Blobs | Report/file storage |
| Azure.Identity + Microsoft.Graph | Azure AD authentication |
| Microsoft.Azure.NotificationHubs | Push notification backend |

## Requirements

- .NET 11.0 or later
- C# 15

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

```bash
git clone https://github.com/CheapNud/CheapHelpers.git
cd CheapHelpers
dotnet restore
dotnet build
```

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.
