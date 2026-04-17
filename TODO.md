<!--
  TODO.md — CheapHelpers project work tracker
  Last updated: 2026-04-17 (legacy Identity 2.3.9 removed — NU1903 cleared)

  RULES FOR AI AGENTS:
  - Update the "Last updated" date above whenever you modify this file
  - Items use checkbox format: - [ ] incomplete, - [x] complete
  - Never remove completed items — they serve as history. Move them to "## Done" when a category gets cluttered.
  - Each item gets ONE line. Details go in sub-bullets indented with 2 spaces.
  - Prefix each item with the date it was added: - [ ] (2026-03-17) Description
  - When completing, change to: - [x] (2026-03-17 → 2026-03-18) Description
  - Tag the SOURCE of each item at the end in brackets:
      [code-todo] = from // TODO comment in source code
      [plan] = from a plan document or planning session
      [bug] = from a bug encountered during dev/deploy
      [audit] = from a code audit or review
      [user] = explicitly requested by the user
  - For [code-todo] items, ALWAYS include file:line reference so devs can navigate directly
  - Categories: Blocking, Planned, Future, Done
  - [voltiq-dep] = required by Voltiq project (../Voltiq) before scaffolding can proceed
  - New items go at the TOP of their category
  - Do not create separate TODO_*.md files — everything goes here
  - Keep it terse. If it needs more than 3 sub-bullets, link to a plan document.
  - Do NOT create, rename, or remove categories — the fixed set is: Blocking, Planned, Future, Done
  - When asked for planned work or TODO analysis, ALWAYS include Future items too — list them below Planned and note them as future work
-->

# TODO

## Blocking

- [x] (2026-04-16 → 2026-04-17) Remove legacy `Microsoft.AspNetCore.Identity 2.3.9` reference from `CheapHelpers.EF.csproj` [voltiq-dep] [audit]
  - Deleted the 2.3.9 PackageReference; added `<FrameworkReference Include="Microsoft.AspNetCore.App" />` so `IServiceCollection.AddIdentity<,>` resolves from the shared framework instead of the legacy NuGet
  - Verified via `dotnet nuget why`: `System.Security.Cryptography.Xml` and `Microsoft.AspNetCore.DataProtection` no longer transitively referenced

- [x] (2026-03-30 → 2026-03-30) `UserService<TUser>` should resolve `IDbContextFactory` for derived context types [voltiq-dep]
  - Added `TContext` generic parameter to `UserRepo<TUser, TContext>`, `UserService<TUser, TContext>`, `CheapAccountController<TUser, TContext>`
  - Consumers pass their derived context: `UserService<VoltiqUser, VoltiqDbContext>`
  - Backward-compat shims preserved for `CheapContext<CheapUser>` default
- [x] (2026-03-30 → 2026-03-30) Add built-in `NullEmailService` as auto-registered fallback [voltiq-dep]
  - `NullEmailService : IEmailService` logs warnings via `Debug.WriteLine` instead of sending
  - Auto-registered in `AddCheapHelpersBlazor` when no email provider configured
- [x] (2026-03-30 → 2026-03-30) `AddCheapHelpersComplete` should accept derived context type parameter [voltiq-dep]
  - `AddCheapHelpersComplete<TUser, TContext>()` and `AddCheapHelpersCompleteWithIdentity<TUser, TContext, TRole>()`
  - Registers derived context + base type bridge so services at all levels resolve correctly
- [x] (2026-03-29 → 2026-03-29) Add protected constructor to `CheapContext<TUser>` for derived context support [voltiq-dep]
  - Added `protected CheapContext(DbContextOptions, CheapContextOptions?)` accepting non-generic options base
  - Converted from primary constructor to regular constructors (primary can't chain to `base` with different options type)
  - Enables: `class VoltiqDbContext(DbContextOptions<VoltiqDbContext> opts) : CheapContext<VoltiqUser>(opts)`
- [x] (2026-03-29 → 2026-03-29) Make AccountController generic — `CheapAccountController<TUser> where TUser : CheapUser` [voltiq-dep]
  - Renamed `AccountController` → `CheapAccountController<TUser>` with generic `SignInManager<TUser>`, `UserManager<TUser>`, `UserService<TUser>`
  - Made `UserRepo<TUser>` generic (was hardcoded to CheapUser), backward-compat `UserRepo` shim preserved
  - Made `UserService<TUser>` generic, backward-compat `UserService` shim preserved
  - Updated DI registration in `AddCheapHelpersBlazor<TUser>` to use `UserService<TUser>`
- [x] (2026-03-28 → 2026-03-28) API key system — configurable prefix support (`GenerateKey("VTQ")` → `VTQ-a7f3bc91...`) [voltiq-dep]
  - Added `prefixOverride` parameter to `GenerateAsync` (null defaults to `ApiKeyOptions.KeyPrefix`)
- [x] (2026-03-28 → 2026-03-28) API key system — hash storage, NEVER store plaintext in DB, `GenerateKey()` returns plaintext once [voltiq-dep]
  - Already implemented: SHA-256 via `ComputeHash()`, `FullKey` only in `ApiKeyCreateResult`
- [x] (2026-03-28 → 2026-03-28) API key system — `IApiKeyValidator<TEntity>` generic validation service (hash + DB lookup) [voltiq-dep]
  - Created `IApiKeyValidator<TEntity>` with `ValidateForEntityAsync` + `ApiKeyEntityValidationResult`
- [x] (2026-03-28 → 2026-03-28) API key system — `MaskKey()` standardized display masking (`xxxx...yyyy`) [voltiq-dep]
  - Added `MaskKey()` instance method + `MaskFullKey(string)` static helper on `ApiKey` entity
- [x] (2026-03-28 → 2026-03-28) API key system — expiry support (`ExpiresAt?` nullable DateTime) for billing tier enforcement [voltiq-dep]
  - Already implemented: `ExpiresAt` property, checked in `IsValid` computed property
- [x] (2026-03-28 → 2026-03-28) API key system — rate limiting metadata per key (requests per minute/hour) [voltiq-dep]
  - Already implemented: `RateLimitPerMinute`, `RateLimitPerDay` on entity, enforced in middleware
- [x] (2026-03-28 → 2026-03-28) API key system — audit trail fields (`CreatedAt`, `LastUsedAt`, `CreatedBy`) [voltiq-dep]
  - Added `CreatedBy` field (nullable, defaults to `UserId`). `CreatedAt`, `LastUsedAt` already existed.
- [x] (2026-03-28 → 2026-03-28) API key system — multiple keys per entity (1:N relationship) [voltiq-dep]
  - Already implemented: `UserId` FK allows multiple, `GetUserKeysAsync()` returns all
- [x] (2026-03-28 → 2026-03-28) API key system — scoped permissions (read-only vs read-write key types) [voltiq-dep]
  - Already implemented: `ScopesJson` stores scope array, deserialized via `Scopes` property, passed as claims
- [ ] (2026-03-28) Publish CheapHelpers NuGet with complete API key system [voltiq-dep]

## Planned
- [x] (2026-03-28 → 2026-03-28) Add API key distribution system tied to user identity [user]
  - `CheapHelpers.Models/Entities/ApiKey.cs` — EF entity with SHA-256 hashed keys, prefix display, scopes JSON, rate limits
  - `CheapHelpers.Services/ApiKeys/` — `IApiKeyService`, `ApiKeyService<TUser>` with generate/validate/revoke/rotate, ConcurrentDictionary cache
  - `CheapHelpers.Blazor/Middleware/ApiKeyMiddleware.cs` — header extraction, validation, sliding window rate limiting (minute + day), claims principal
  - `CheapContext.OnModelCreating` — unique index on KeyHash, composite on (UserId, IsActive)
- [x] (2026-03-28 → 2026-03-28) Add GitHub and Apple OAuth providers to CheapHelpers auth [user]
  - `GitHubAuthOptions` with `EnterpriseDomain`, `AppleAuthOptions` with `TeamId`, `KeyId`, `PrivateKeyPath`/`PrivateKeyContent`
  - Extended `OAuthBlazorExtensions` with `AddGitHubAuth()`, `AddAppleAuth()`, refactored `MapOAuthEndpoints` to shared `MapProviderEndpoints`
  - Added `AspNet.Security.OAuth.GitHub` v10.0.0, `AspNet.Security.OAuth.Apple` v10.0.0
- [ ] (2026-03-28) Add generic TTS abstraction to CheapHelpers.Services [user]
  - `CheapHelpers.Services/Voice/` — `ITextToSpeech`, `TtsOptions`, DI extension
  - Edge TTS provider (free, WebSocket-based, no API key) — extract from CheapCOVAS
  - Azure Cognitive Services TTS provider (production quality)
- [ ] (2026-03-28) Add generic STT abstraction to CheapHelpers.Services [user]
  - `CheapHelpers.Services/Voice/` — `ISpeechToText`, `SttResult`, `SttOptions`, DI extension
  - Whisper provider (OpenAI-compatible, works with local whisper.cpp) — extract from CheapCOVAS
  - Azure Speech Services provider
- [x] (2026-03-28 → 2026-03-28) Add logging to NotificationBell empty catch blocks [audit]
  - Injected `ILogger<NotificationBell>`, replaced 5 silent catch blocks with `LogWarning`/`LogError`
- [x] (2026-03-28 → 2026-03-28) Add null/bounds validation to `CollectionExtensions.Replace` methods [audit]
  - Added `ArgumentNullException` guards, not-found checks, multi-match detection for predicate overload
- [x] (2026-03-17 → 2026-03-28) Add `SanitizeFileName()` to `StringExtensions` (CheapHelpers) [audit]
  - Added `Path.GetInvalidFileNameChars()`-based method with configurable replacement char
- [x] (2026-03-17 → 2026-03-28) Add shared Plex SSO auth provider [user]
  - `CheapHelpers.Services/Auth/Plex/` — IPlexAuthService, PlexAuthService, PlexUser, PlexPin, PlexAuthOptions
  - `IExternalAuthProvider` marker interface, `IExternalUserProvisioner` bridge with opt-in `AddExternalUserProvisioning<TUser>()`
  - `PlexAuthBlazorExtensions.MapPlexAuthEndpoints()` for `/auth/plex-start`, `/auth/plex-callback`, `/auth/logout`
- [x] (2026-03-17 → 2026-03-28) Refactor `AsyncLazy<T>` for HardwareDetectionService caching [code-todo]
  - Created `CheapHelpers/Threading/AsyncLazy.cs` — generic async lazy with `GetAwaiter` support
  - Replaced volatile + SemaphoreSlim double-check pattern in both Windows and Linux services
- [ ] (2026-03-17) Implement Redis caching support for notifications [code-todo]
  - `CheapHelpers.Services/Notifications/Extensions/NotificationServiceExtensions.cs:98`
- [x] (2026-03-17 → 2026-03-28) Implement RabbitMQ real-time notification support [code-todo]
  - `RabbitMQNotificationRealTimeService` publishes to topic exchange, routing by `notification.user.{id}` and `notification.broadcast`
  - `RabbitMQNotificationConsumer` (BackgroundService) subscribes and forwards to local SignalR hub
  - SignalR remains default and client-facing transport; RabbitMQ enables cross-server delivery
  - Opt-in via `RealTimeProvider = "RabbitMQ"` + `AddCheapNotificationsRabbitMQConsumer(connectionString)`
- [x] (2026-03-17 → 2026-03-28) Implement WebViewStorageBridge registration [code-todo]
  - Created `WebViewStorageBridge<TData>` with JS interop for localStorage/sessionStorage/cookies
  - Polling-based change monitoring with `DataChanged` event, uses `WebViewJsonParser` for deserialization
  - Wired DI in `AddWebViewBridge<TData>()` with `WebViewStorageBridgeConfig`
- [x] (2026-03-17 → 2026-03-28) Implement Azure Notification Hub backend [code-todo]
  - `CheapHelpers.Blazor/Hybrid/Notifications/Backends/AzureNotificationHubBackend.cs` — implements `IPushNotificationBackend`
  - Wired `UseAzureNotificationHubs(connectionString, hubName)` in `PushNotificationOptions` (was `NotImplementedException`)
  - Device registration via Installation API, tag-based + device-targeted sending, FCMv1/APNS payloads
  - Added `Microsoft.Azure.NotificationHubs` v4.2.0
- [x] (2026-03-17 → 2026-03-28) Implement iLovePDF API for PdfOptimizationService [code-todo]
  - File path overload: `CreateTask<CompressTask>` → `AddFile` → `Process` → `DownloadFile`
  - Stream overload: uploads byte array, downloads via `DownloadFileAsByteArrayAsync`
  - Maps `PdfOptimizationLevel` → `CompressionLevels` (Low/Recommended/Extreme)
  - `IsILovePdfAvailable` now returns true when API credentials configured
- [x] (2026-03-21 → 2026-03-28) Clean up `JsonService` — old implementation commented out, marked "fix and cleanup" [code-todo]
  - Removed dead commented-out code, modernized to file-scoped namespace and using declarations
- [x] (2026-03-21 → 2026-03-28) Replace legacy email HTML templating with a templating engine [code-todo]
  - Migrated `SendEmailConfirmationAsync`, `SendPasswordTokenAsync`, `SendDeveloperAsync` to Fluid/Liquid templates
  - Created `EmailConfirmationTemplateData`, `PasswordResetTemplateData`, `ExceptionReportTemplateData` + `.liquid` files
  - Removed `ToHtmlString`, `FormatExceptionReportAsHtml`, `AppendExceptionAsHtml` and all `[Obsolete]` attributes
- [x] (2026-03-21 → 2026-03-28) Make notification cleanup interval configurable via `NotificationOptions` [code-todo]
  - Added `CleanupIntervalMinutes` property (default 60), wired into `NotificationCleanupService`
- [x] (2026-03-21 → 2026-03-28) Use user timezone instead of UTC for Do Not Disturb [code-todo]
  - Queries `CheapUser.TimeZoneInfoId` from DB, converts UTC to user's local time for DND check
  - Tightened generic constraint from `IdentityUser` to `CheapUser` on provider + DI extension

## Future
- [x] (2026-03-28 → 2026-03-29) Add billing service with PEPPOL BIS 3.0 invoicing [user]
  - UBL Invoice/CreditNote DTOs + PEPPOL constants (BIS 3.0 CustomizationId, ProfileId, Belgian endpoint schemes)
  - `UblInvoiceService` with `CreateInvoiceAsync`/`CreateCreditNoteAsync` using UblSharp InvoiceType/CreditNoteType
  - `InvoiceBuilder` / `CreditNoteBuilder` fluent API — hides UBL complexity, auto-calculates taxes and totals
  - `UblPartyMapper` extracted from UblService (shared party/address/contact conversion)
  - `PeppolInvoiceValidator` with Belgian VAT/enterprise number validation
  - `IUsageMeterService` + `IBillingService` with metered billing, usage aggregation, invoice generation
  - `BillingPlan`, `BillingInvoice`, `UsageRecord`, `UsageAggregate` entities with EF configuration
  - `AddCheapBilling<TUser>()` DI extension
- [x] (2026-03-28 → 2026-03-29) Add reporting service with full pipeline (generate, store, distribute) [user]
  - `IReportService` with PDF/Excel generation, storage, download, expiration cleanup
  - `IReportStorageProvider` with `AzureBlobReportStorageProvider` and `LocalFileReportStorageProvider`
  - `IReportDistributionService` for email distribution with attachments
  - `IScheduledReportService` wrapping IScheduledTaskService for recurring reports
  - `Report` entity with lifecycle tracking (Queued → Generating → Completed/Failed → Expired)
  - `AddCheapReporting<TUser>()` DI extension with configurable storage provider
- [ ] (2026-03-28) Complete `ImagePanel.AnalyzeImageAsync` Azure Vision integration [code-todo]
  - `CheapHelpers.Blazor/Shared/ImagePanel.razor:295` — skeleton with example code, waiting for VisionServiceOptions config
- [x] (2026-03-28 → 2026-03-28) Consider Humanizer library for NotificationBell timestamps [code-todo]
  - Replaced manual `HumanizeTimestamp` with `Humanizer.Core` (v2.14.1) `DateTime.Humanize()`
- [x] (2026-03-28 → 2026-03-28) Move validation message constants to .resx for localization [code-todo]
  - Added user-facing resource keys to `Language.resx`, added `*Key` constants alongside existing internal message strings
- [ ] (2026-03-28) Add MCP (Model Context Protocol) tool hosting abstraction [user]
  - `IMcpToolHost` — expose CheapHelpers services (barcode, PDF, email, etc.) as MCP tools
  - MCP is still evolving — monitor spec stability before implementing

## Done

- [x] (2026-03-28 → 2026-03-28) Add SendGrid email service implementation [user]
  - `CheapHelpers.Services/Email/SendGridEmailService.cs` — implements `IEmailService` using SendGrid API
  - Follows same pattern as MailKitEmailService/GraphService: primary constructor, dev mode override, attachment support
  - Added `SendGrid` NuGet package v9.29.3
- [x] (2026-03-28 → 2026-03-28) Finish and flesh out Blazor account module [user]
  - Fixed Register.razor: uses CheapUser via UserFactory parameter, wired RegisterValidator, localized, removed MoreLinq
  - Fixed Authenticator.razor: removed hardcoded localhost HttpClient, injected UserManager directly, extracted shared AuthenticatorHelper
  - Implemented ConfirmEmailChange.razor: email change confirmation with ChangeEmailAsync flow
  - Rebuilt Notifications.razor: generic notification list with server-side paging + notification preferences management
  - Cleaned LoginDisplay.razor: localized, generic notification count, simplified avatar
  - Enabled CustomTabs in Index.razor: pre-filtered async authorization, sorted by Order
  - Added generic external auth provisioning bridge: IExternalUserProvisioner, ExternalUserInfo, ExternalProvisionResult
  - Default ExternalUserProvisioner with find-or-create logic, opt-in via AddExternalUserProvisioning<TUser>()
  - Hooked into PlexAuthBlazorExtensions for optional Identity session provisioning
- [x] (2026-03-28 → 2026-03-28) Remove obsolete methods and rename static IV encryption [audit]
  - Removed `TokenRefreshed` event from interface + iOS/Android implementations, migrated `WaitForTokenAsync` to use `OnTokenReceived`/`OnTokenUpdated`
  - Removed sync `Send()` from `ISmsService`/`TwilioSmsService`
  - Removed sync `GetBarcode`, `OnScan`, `ReadBarcodeAsync(bytes, width, height)` from `IBarcodeService`/`BarcodeService`
  - Renamed `Encrypt`/`Decrypt` → `EncryptDeterministic`/`DecryptDeterministic` (consumers need deterministic encryption for URL routes)
  - Updated `EncryptedRouteConstraint` and `CustomNavigationService` to use renamed methods, removed pragma suppressions
- [x] (2026-03-28 → 2026-03-28) Delete dead code and orphaned comments [audit]
  - Deleted `WebExceptionHelper.cs` (Dutch-localized legacy error strings)
  - Deleted `ExampleProgram.cs` and `ExampleStartup.cs` (entirely commented-out legacy Startup pattern)
  - Removed "EXAMPLE DO NOT USE" commented code from `EnumExtensions.cs`
  - Removed orphaned `//new Thread(...)` from `CollectionExtensions.cs`
  - Removed commented `SearchProductAsync` from `SearchDialogService.cs`
- [x] (2026-03-27 → 2026-03-28) Extract generic `IMdnsDiscoveryService` from existing mDNS code [plan]
  - `CheapHelpers.Networking/Discovery/` — IMdnsDiscoveryService, MdnsDiscoveryService, MdnsDevice, MdnsDiscoveryOptions
  - Swapped Makaretu → MeaMod.DNS, refactored MdnsDetector to consume shared service
  - Persistent listener, split A/AAAA caching, TXT parsing inspired by Calculus GatewayDiscovery
- [x] (2026-03-24 → 2026-03-24) Add `TimeWindow` value type to CheapHelpers.Models [voltiq-dep]
  - `CheapHelpers.Models/ValueTypes/TimeWindow.cs` — record struct with `Duration`, `Contains`, `Overlaps`, `Intersect`, `Current`, `ForInterval`, `Enumerate`
- [x] (2026-03-24 → 2026-03-24) Add unit conversion extension methods to CheapHelpers [voltiq-dep]
  - `CheapHelpers/Extensions/UnitConversionExtensions.cs` — Power, Energy, Volume (double + decimal)
- [x] (2026-03-24 → 2026-03-24) Add `IHttpPollingService` to CheapHelpers.Services [voltiq-dep]
  - `CheapHelpers.Services/Polling/` — interface, impl with PeriodicTimer + exponential backoff, options, DI extension
- [x] (2026-03-24 → 2026-03-24) Add `IScheduledTaskService` to CheapHelpers [voltiq-dep]
  - `CheapHelpers/Scheduling/` — interface, BackgroundService impl with interval/daily/monthly, DI extension
  - Added `Microsoft.Extensions.Hosting.Abstractions` to CheapHelpers.csproj
- [x] (2026-03-24 → 2026-03-24) Add `IDeviceHealthCheck` to CheapHelpers.Services [voltiq-dep]
  - `CheapHelpers.Services/Health/` — IDeviceHealthCheck, IDeviceHealthMonitor, DeviceHealthResult record, hosted monitor with transition detection, DI extension
- [x] (2026-03-17 → 2026-03-24) Fix CheapContext `DATETIME('now')` per-provider detection (CheapHelpers.EF) [bug] [voltiq-dep]
  - `CheapContext.GetUtcNowFunction()` now switches on `Database.ProviderName` — SQLite, SQL Server, Npgsql
  - Added `Constants.Database.NpgsqlUtcNowFunction` and `Constants.Database.ProviderNames` static class
