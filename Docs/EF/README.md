# CheapHelpers.EF Documentation

Entity Framework Core helpers. Other pages in this folder: [ContextExtensions](ContextExtensions.md), [PaginatedList](PaginatedList.md), [Repository](Repository.md), [Jobs](Jobs.md).

The `CheapHelpers.EF.Jobs` namespace holds a persistent job queue that maps a job table into your own context, see [Jobs](Jobs.md).

## Startup helpers

### MigrateOrEnsureCreatedAsync

Brings the database up to date at startup. When the context has migrations, it logs the pending ones by name and runs `MigrateAsync`. Without migrations it runs `EnsureCreatedAsync`. The context comes from a new scope, or from `IDbContextFactory<TContext>` when only the factory is registered. Messages go to the `CheapHelpers.EF.DatabaseInitialization` logger, or to `Debug.WriteLine` when no logging is registered.

```csharp
using CheapHelpers.EF.Extensions;

var app = builder.Build();
await app.MigrateOrEnsureCreatedAsync<AppDbContext>();
```

It works on any `IHost` and on any `IServiceProvider`.

### UseSqliteWal

Switches SQLite databases to WAL journal mode, so reads keep working while a write is in progress. The `SqliteWalInterceptor` sends `PRAGMA journal_mode=WAL;` on the first open of each connection string and skips it after that, because the mode is stored in the database file. Read-only connections are skipped. Connections of other providers are left alone, so the call is safe when the provider is chosen at runtime. The package does not depend on SQLite.

```csharp
using CheapHelpers.EF.Extensions;

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(cs).UseSqliteWal());
```

A database file that is deleted and recreated under the same connection string keeps the default journal mode until the process restarts. `EnsureCreated` on SQLite already sets WAL itself when it creates the file.
