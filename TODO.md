<!--
  TODO.md — CheapHelpers project work tracker
  Last updated: 2026-03-28 (TTS/STT/MCP added)

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

_Nothing blocking._

## Planned
- [ ] (2026-03-28) Add generic TTS abstraction to CheapHelpers.Services [user]
  - `CheapHelpers.Services/Voice/` — `ITextToSpeech`, `TtsOptions`, DI extension
  - Edge TTS provider (free, WebSocket-based, no API key) — extract from CheapCOVAS
  - Azure Cognitive Services TTS provider (production quality)
- [ ] (2026-03-28) Add generic STT abstraction to CheapHelpers.Services [user]
  - `CheapHelpers.Services/Voice/` — `ISpeechToText`, `SttResult`, `SttOptions`, DI extension
  - Whisper provider (OpenAI-compatible, works with local whisper.cpp) — extract from CheapCOVAS
  - Azure Speech Services provider
- [ ] (2026-03-28) Add logging to NotificationBell empty catch blocks [audit]
  - `CheapHelpers.Blazor/Components/NotificationBell.razor:205,225,272,308,370` — 4 silent exception swallows
- [ ] (2026-03-28) Add null/bounds validation to `CollectionExtensions.Replace` methods [audit]
  - `CheapHelpers/Extensions/CollectionExtensions.cs:36-38` — comments outline missing checks
- [ ] (2026-03-17) Add `SanitizeFileName()` to `StringExtensions` (CheapHelpers) [audit]
  - `Path.GetInvalidFileNameChars()`-based — more correct than generic `Sanitize()` for file paths
  - CheapManga.DownloadService has this as a private method currently
- [ ] (2026-03-17) Add shared Plex SSO auth provider [user]
  - New folder: `CheapHelpers/Auth/Plex/` — PlexAuthProvider, PlexUser, PlexPin
  - Design `IExternalAuthProvider` interface for future Google/Discord/etc.
  - Endpoint mapper helper for `/auth/plex-start`, `/auth/plex-callback`, `/auth/logout`
  - Consumers: CheapManga, CheapNights (both currently have their own copies)
- [ ] (2026-03-17) Refactor `AsyncLazy<T>` for HardwareDetectionService caching [code-todo]
  - `CheapHelpers.MediaProcessing/Services/HardwareDetectionService.cs:38`
  - `CheapHelpers.MediaProcessing/Services/Linux/LinuxHardwareDetectionService.cs:34`
- [ ] (2026-03-17) Implement Redis caching support for notifications [code-todo]
  - `CheapHelpers.Services/Notifications/Extensions/NotificationServiceExtensions.cs:98`
- [ ] (2026-03-17) Implement RabbitMQ real-time notification support [code-todo]
  - `CheapHelpers.Services/Notifications/Extensions/NotificationServiceExtensions.cs:113`
- [ ] (2026-03-17) Implement WebViewStorageBridge registration [code-todo]
  - `CheapHelpers.Blazor/Hybrid/Extensions/BlazorHybridServiceExtensions.cs:51`
- [ ] (2026-03-17) Implement Azure Notification Hub backend [code-todo]
  - `CheapHelpers.Blazor/Hybrid/Extensions/BlazorHybridServiceExtensions.cs:141`
- [ ] (2026-03-17) Implement iLovePDF API for PdfOptimizationService [code-todo]
  - `CheapHelpers.Services/DataExchange/Pdf/PdfOptimizationService.cs:25`
- [ ] (2026-03-21) Clean up `JsonService` — old implementation commented out, marked "fix and cleanup" [code-todo]
  - `CheapHelpers.Services/DataExchange/Json/JsonService.cs:3`
- [ ] (2026-03-21) Replace legacy email HTML templating with a templating engine [code-todo]
  - `CheapHelpers.Services/Email/EmailExtensions.cs:67` — marked `[Obsolete]`
  - Manual string building for exception emails, needs proper templating
- [ ] (2026-03-21) Make notification cleanup interval configurable via `NotificationOptions` [code-todo]
  - `CheapHelpers.Services/Notifications/NotificationCleanupService.cs:45` — hardcoded to 1 hour
- [ ] (2026-03-21) Use user timezone instead of UTC for Do Not Disturb [code-todo]
  - `CheapHelpers.Services/Notifications/Subscriptions/GlobalUserPreferencesProvider.cs:115`

## Future

- [ ] (2026-03-28) Add API key distribution system tied to user identity [user]
  - Key generation, rotation, revocation, per-user scoping
  - Rate limiting and usage tracking per key
- [ ] (2026-03-28) Add billing service attached to API key usage [user]
  - Metered billing based on API call volume
  - Ties into API key distribution system
- [ ] (2026-03-28) Add reporting service tied to PDF/mailing [user]
  - Generate and distribute reports via PDF export and email
  - Integrate with existing `EmailTemplateService` and `PdfOptimizationService`
- [ ] (2026-03-28) Complete `ImagePanel.AnalyzeImageAsync` Azure Vision integration [code-todo]
  - `CheapHelpers.Blazor/Shared/ImagePanel.razor:295` — skeleton with example code, waiting for VisionServiceOptions config
- [ ] (2026-03-28) Consider Humanizer library for NotificationBell timestamps [code-todo]
  - `CheapHelpers.Blazor/Components/NotificationBell.razor:406` — manual relative time formatting
- [ ] (2026-03-28) Move validation message constants to .resx for localization [code-todo]
  - `CheapHelpers/Constants/Constants.cs:452`
- [ ] (2026-03-28) Add MCP (Model Context Protocol) tool hosting abstraction [user]
  - `IMcpToolHost` — expose CheapHelpers services (barcode, PDF, email, etc.) as MCP tools
  - MCP is still evolving — monitor spec stability before implementing

## Done

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
