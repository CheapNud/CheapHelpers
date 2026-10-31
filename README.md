# CheapHelpers

A collection of production-ready C# utilities, extensions, and services for .NET 11.0 development. Simplify common development tasks with battle-tested helpers for Blazor, Entity Framework, networking, email, PDF generation, billing, reporting, and more.

## Packages

| Package | Version | Description |
|---------|---------|-------------|
| [CheapHelpers](https://www.nuget.org/packages/CheapHelpers) | ![NuGet](https://img.shields.io/nuget/v/CheapHelpers.svg) | Core utilities, extensions, threading, scheduling, and the shared entities, DTOs and enums |
| [CheapHelpers.EF](https://www.nuget.org/packages/CheapHelpers.EF) | ![NuGet](https://img.shields.io/nuget/v/CheapHelpers.EF.svg) | Entity Framework repository pattern with Identity support and a persistent job queue |
| [CheapHelpers.Services](https://www.nuget.org/packages/CheapHelpers.Services) | ![NuGet](https://img.shields.io/nuget/v/CheapHelpers.Services.svg) | Email, PDF, billing, reporting, notifications, and integrations, network scanning and device detection |
| [CheapHelpers.Barcodes](https://www.nuget.org/packages/CheapHelpers.Barcodes) | ![NuGet](https://img.shields.io/nuget/v/CheapHelpers.Barcodes.svg) | Barcode generation and reading on ZXing.Net with no native image dependency |
| [CheapHelpers.Auth](https://www.nuget.org/packages/CheapHelpers.Auth) | ![NuGet](https://img.shields.io/nuget/v/CheapHelpers.Auth.svg) | Plex and OAuth sign-in, API keys, shared-secret auth, forwarded headers and Data Protection setup |
| [CheapHelpers.Llm](https://www.nuget.org/packages/CheapHelpers.Llm) | ![NuGet](https://img.shields.io/nuget/v/CheapHelpers.Llm.svg) | LLM client for Anthropic and OpenAI-compatible providers with structured output |
| [CheapHelpers.Blazor](https://www.nuget.org/packages/CheapHelpers.Blazor) | ![NuGet](https://img.shields.io/nuget/v/CheapHelpers.Blazor.svg) | Blazor components, account module, and Hybrid features |
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

# Plex, OAuth and OpenID Connect sign-in, API keys, forwarded headers
dotnet add package CheapHelpers.Auth

# Blazor components, account module, and Hybrid features
dotnet add package CheapHelpers.Blazor

# Network scanning and device discovery
dotnet add package CheapHelpers.Services
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
In CheapHelpers.Auth; full setup in [Docs/Auth](Docs/Auth/README.md).
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

// Simple API — fluent builder, no UBL knowledge needed
var invoice = InvoiceBuilder.Create("INV-2026-000001")
    .From("My Company", vatNumber: "BE0123456789")
    .To("Acme Corp", vatNumber: "BE9876543210")
    .DueIn(days: 30)
    .AddLine("API calls - March 2026", quantity: 50000, unitPrice: 0.001m, vatPercent: 21)
    .AddLine("Premium support", quantity: 1, unitPrice: 150.00m, vatPercent: 21)
    .WithPayment(iban: "BE68539007547034", bic: "BBRUBEBB")
    .Build();
// Tax totals, line totals, and monetary totals are calculated automatically

var invoiceService = new UblInvoiceService();
var xml = await invoiceService.CreateInvoiceXmlAsync(invoice);

// Credit notes work the same way
var creditNote = CreditNoteBuilder.Create("CN-2026-000001")
    .CreditsInvoice("INV-2026-000001")
    .WithReason("Overcharged API calls")
    .From("My Company", vatNumber: "BE0123456789")
    .To("Acme Corp", vatNumber: "BE9876543210")
    .AddLine("API call correction", quantity: 10000, unitPrice: 0.001m, vatPercent: 21)
    .Build();

// Advanced: use UblInvoice DTOs directly for full PEPPOL control
var advancedInvoice = new UblInvoice { /* full UBL model access */ };
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
In CheapHelpers.Auth; full setup, including shared-secret auth, forwarded headers and Data Protection, in [Docs/Auth](Docs/Auth/README.md).
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
    // The Register page answers with the router's not-found content; the app creates accounts itself, for example through IUserService
    options.RegistrationEnabled = false;
});

// Map the account and culture endpoints (after app.UseAntiforgery()):
app.MapCheapAccountEndpoints<MyUser>();
app.MapCheapCultureEndpoint();

// Built-in pages: Login, Register, ForgotPassword, ResetPassword,
// ConfirmEmail, ConfirmEmailChange, SetPassword, ChangePassword,
// Authenticator (2FA), RecoveryCodes, PersonalData (GDPR), Lockout
// Account dashboard with extensible custom tabs
```

#### Migrating from the account controller
Delete your `CheapAccountController<TUser, TContext>` subclass and its `[Route]` attribute. Call `app.MapCheapAccountEndpoints<TUser>()` and `app.MapCheapCultureEndpoint()` after `app.UseAntiforgery()`. Sign-out is now POST only, so replace any sign-out link with a `<form method="post">` that contains `<AntiforgeryToken />`. A forced redirect after sign-in, such as a password change, goes in the `AfterSignIn` option instead of an overridden action. `OAuthProviderOptions.LoginPath` and `PlexAuthOptions.LoginPath` now default to `/Account/Login`, so an app that relied on the old `/login` default must set them explicitly. The barcode types moved to the `CheapHelpers.Barcodes` namespace and package, `GetBarcodeAsync` is now `GetBarcode` and returns PNG, and reading an image file is the `ReadBarcode(byte[])` extension in CheapHelpers.Services. ImageSharp is gone and NetVips (with NetVips.Native) now comes in through CheapHelpers.Services, so an app that referenced ImageSharp only for this library can drop it. NetVips.Native has no BMP loader, so `ReadBarcode(byte[])` rejects BMP files and uploads keep BMP files as they are instead of converting them to JPEG. CheapHelpers.Blazor no longer depends on Blazored.LocalStorage, so an app that registered it only for this library can remove the `AddBlazoredLocalStorage` call. `BlobService` uploads now store the content type and `GetFileUri` no longer writes blob headers, so blobs uploaded before 4.0.0 keep whatever content type they have (browsers sniff images, and a one-off `SetHttpHeaders` pass fixes any that matter). `GetFileUri` and `GetFile` no longer contact storage at all, so a file name whose blob is missing now gets a URL that returns 404 instead of the placeholder. `ProgressButton` no longer needs `AddCheapHelpersBlazor` for its localizer, so it works in an app that only calls `AddMudServices`. `FileSettingsService` now throws an `InvalidOperationException` on a stored value it cannot deserialise instead of returning the default, and setting `OnCorruptValue = CorruptValueHandling.ReturnDefault` in `AddFileSettingsService` brings the old behaviour back. `IBackgroundTaskQueue.QueueBackgroundWorkItemAsync` and `GetExecutableVersionAsync` on both executable detection services take a trailing `CancellationToken`, and the `BackgroundTaskQueue` capacity is now an `int?` where null means unbounded, so implementations of the interface need the new parameter and existing binaries need a rebuild. The Plex, OAuth, OpenID Connect and API key types moved from CheapHelpers.Services and CheapHelpers.Blazor to the CheapHelpers.Auth package, under the `CheapHelpers.Auth`, `CheapHelpers.Auth.Plex`, `CheapHelpers.Auth.OAuth` and `CheapHelpers.Auth.ApiKeys` namespaces; the `AddXxxAuth`, `MapPlexAuthEndpoints`, `MapOAuthEndpoints`, `AddCheapApiKeys` and `UseCheapApiKeyMiddleware` methods keep their names and now live in the `CheapHelpers.Auth` namespace. CheapHelpers.Blazor references CheapHelpers.Auth, and since CheapHelpers.Auth does not depend on CheapHelpers.Services, an app that only needs Plex or OAuth sign-in without the Blazor components can reference CheapHelpers.Auth alone. `CsvService` exports now prefix cells that start with `=`, `+`, `-`, `@`, tab or CR and are not plain numbers with a `'` to stop spreadsheet formula injection.

CheapHelpers.Blazor 4.0.1 fixes a startup crash in published apps that have a `wwwroot` file with the same relative name as one of the package's assets, for example `js/site.js`. The static asset manifest format that 4.0.0 shipped with caused it. Apps on 4.0.0 can rename the clashing file or upgrade. In 4.0.2 the boot script no longer shows a retry counter and no longer reloads the page on its own; once the retries fail, the `reconnect-modal` element shows a Refresh button.

CheapHelpers.Llm 4.0.2 moves to Anthropic SDK 13.x, so apps that reference the Anthropic package themselves or pass `ProviderBlocks` must take 13.x too; the `ILlmClient` surface is unchanged. CheapHelpers.Llm 4.0.3 adds opt-in input hygiene (`StripInvisibleCharacters`, `MaxInputCharacters`).

CheapHelpers.Auth 4.0.1 enforces `HostedDomain` on the server with Google's `hd` claim and rejects sign-ins that carry no email; CheapHelpers.Blazor 4.0.4 only moves to that Auth version. The other packages move their Microsoft dependencies to 10.0.14.

To force a password change, let `AfterSignIn` return `routes.ChangePasswordRoute + "?forced=true"` when your own check says the user needs a new password. The change-password page then shows a warning that a new password is required. Keeping the user on that page until the password is changed stays in your app, for example in middleware that redirects other requests. `AfterPasswordChanged` runs after a successful change on that page, so that is where the app clears its own flag and stamps its changed-at date; return a path to redirect somewhere other than `HomeRoute`. The check in `AfterSignIn` must skip users without a password (`await userManager.HasPasswordAsync(user)` is false for accounts that only sign in externally), because they have nothing to change and would loop on the page.

```csharp
// MustChangePassword and PasswordChangedAt are the app's own MyUser properties
var routes = app.Services.GetRequiredService<AccountRouteOptions>();
app.MapCheapAccountEndpoints<MyUser>(options =>
{
    options.AfterSignIn = async (user, httpContext) =>
    {
        var userManager = httpContext.RequestServices.GetRequiredService<UserManager<MyUser>>();
        return user.MustChangePassword && await userManager.HasPasswordAsync(user)
            ? routes.ChangePasswordRoute + "?forced=true"
            : null;
    };
    options.AfterPasswordChanged = async (user, httpContext) =>
    {
        var userManager = httpContext.RequestServices.GetRequiredService<UserManager<MyUser>>();
        user.MustChangePassword = false;
        user.PasswordChangedAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);
        return null;
    };
});
```

Passkeys appear on the login page and in the Security tab of the account page once the app sets `Stores.SchemaVersion = IdentitySchemaVersions.Version3` in its Identity options. Add one EF migration for the new `AspNetUserPasskeys` table. Nothing else is needed, because the package loads its passkey script on every page itself.

### Entity Framework
```csharp
// Generic context with Identity
services.AddCheapContext<MyUser>(options => options.UseSqlServer(connectionString))
    .AddIdentity<IdentityRole>();

// UserRepo<TUser, TContext> needs Identity's hasher, normalizer, options and error describer, so don't construct it:
// inject UserService<TUser, TContext> (a UserRepo, registered by AddCheapHelpersBlazor) from DI
var userRepo = serviceProvider.GetRequiredService<UserService<MyUser, CheapContext<MyUser>>>();

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
CheapHelpers/                      Core utilities, extensions, scheduling, threading; Models/ holds entities, DTOs, enums, UBL invoice models
CheapHelpers.EF/                   Entity Framework, CheapContext, repositories
CheapHelpers.Auth/                 Plex, OAuth/OIDC, shared secret, forwarded headers, Data Protection
  ├── ApiKeys/                     API key generation, validation, rate-limiting middleware
  ├── OAuth/                       Google, Microsoft, GitHub, Apple, OpenID Connect options
  └── Plex/                        Plex PIN sign-in service
CheapHelpers.Services/
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
  ├── Networking/                  Network scanner, mDNS discovery, device detectors
  ├── Notifications/               Multi-channel (InApp, Email, SMS, Push) + RabbitMQ
  ├── Polling/                     HTTP polling with exponential backoff
  ├── Reporting/                   Report generation, storage, distribution, scheduling
  └── Storage/                     Azure Blob Storage
CheapHelpers.Blazor/
  ├── Components/                  NotificationBell, CultureSelector
  ├── Extensions/                  DI, account, culture and notification endpoint mapping
  ├── Helpers/                     AuthenticatorHelper, AccountTabDefinition
  ├── Hybrid/                      WebView bridge, push notifications, app bar
  ├── Pages/Account/               Full account module (login, register, 2FA, GDPR)
  ├── Services/                    UserService, ExternalUserProvisioner, RabbitMQ consumer
  └── Shared/                      LoginDisplay, PlexSignInCard, UploadFile, ImagePanel, Selectors
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
