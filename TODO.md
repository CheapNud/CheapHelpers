<!--
  TODO.md — CheapHelpers project work tracker
  Last updated: 2026-03-28 (mDNS moved to Planned, new rules added)

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
- [ ] (2026-03-27) Extract generic `IMdnsDiscoveryService` from existing mDNS code [plan]
  - CheapHelpers.Networking already has `MdnsDetector` (Makaretu.Dns.Multicast.New) but it's only a device type classifier, not a standalone discovery service
  - Calculus.DeviceManagement has battle-tested `GatewayDiscovery` (MeaMod.DNS) with persistent listener, GUID caching from split A/AAAA responses, TXT record parsing — WPF-coupled, needs extracting
  - Goal: generic `IMdnsDiscoveryService` — `DiscoverAsync(string serviceType, CancellationToken)` → `List<MdnsDevice>` with IP, name, TXT records
  - Consolidate on one mDNS library (evaluate Makaretu vs MeaMod — both work, pick one)
  - Voltiq use case: HomeWizard P1 meters advertise via `_hwenergy._tcp.local` — auto-discover meter IP for Tier 2 plug-and-play
  - Consumers: Voltiq (HomeWizard), CheapHelpers.Networking (upgrade MdnsDetector), CheapCOVAS (Elite Dangerous companion)
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
- [ ] (2026-03-21) Clean up or remove `WebExceptionHelper` — flagged as potentially obsolete [code-todo]
  - `CheapHelpers/Helpers/Web/WebExceptionHelper.cs:6`
- [ ] (2026-03-21) Clean up `JsonService` — old implementation commented out, marked "fix and cleanup" [code-todo]
  - `CheapHelpers.Services/DataExchange/Json/JsonService.cs:3`
- [ ] (2026-03-21) Replace legacy email HTML templating with a templating engine [code-todo]
  - `CheapHelpers.Services/Email/EmailExtensions.cs:67` — marked `[Obsolete]`
  - Manual string building for exception emails, needs proper templating
- [ ] (2026-03-21) Make notification cleanup interval configurable via `NotificationOptions` [code-todo]
  - `CheapHelpers.Services/Notifications/NotificationCleanupService.cs:45` — hardcoded to 1 hour
- [ ] (2026-03-21) Use user timezone instead of UTC for Do Not Disturb [code-todo]
  - `CheapHelpers.Services/Notifications/Subscriptions/GlobalUserPreferencesProvider.cs:115`
- [ ] (2026-03-21) Remove obsolete `OnTokenRefresh` property (replaced by `OnTokenReceived`/`OnTokenUpdated`) [audit]
  - `CheapHelpers.MAUI/Platforms/Android/DeviceInstallationService.cs:36`
  - `CheapHelpers.MAUI/Platforms/iOS/DeviceInstallationService.cs:63`
  - `CheapHelpers.Blazor/Hybrid/Abstractions/IDeviceInstallationService.cs:66`
- [ ] (2026-03-21) Remove obsolete sync `Send()` from SMS service (replaced by `SendAsync`) [audit]
  - `CheapHelpers.Services/Communication/Sms/ISmsService.cs:20`
  - `CheapHelpers.Services/Communication/Sms/TwilioSmsService.cs:72`
- [ ] (2026-03-21) Remove obsolete sync barcode methods (replaced by async versions) [audit]
  - `CheapHelpers.Services/Communication/Barcode/IBarcodeService.cs:34,47,63`
  - `CheapHelpers.Services/Communication/Barcode/BarcodeService.cs:85,124,170`
- [ ] (2026-03-21) Remove obsolete `Encrypt`/`Decrypt` with static IV (replaced by random IV versions) [audit]
  - `CheapHelpers/Helpers/Encryption/EncryptionHelper.cs:85,121`

## Future

_Nothing in future._

## Done

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
