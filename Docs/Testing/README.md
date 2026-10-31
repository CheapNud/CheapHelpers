# CheapHelpers.Testing Documentation

Test doubles for apps on CheapHelpers: an in-memory SQLite database, a recording HTTP handler, header-driven test authentication, captured logs and a test web host. Each one replaces a helper that test projects otherwise write by hand.

## Package Information

- **Package**: CheapHelpers.Testing
- **Target Framework**: .NET 11.0 (net11.0)
- **NuGet**: [CheapHelpers.Testing](https://www.nuget.org/packages/CheapHelpers.Testing)

## SqliteTestDatabase

One in-memory SQLite connection per test. Every context shares it, so they see the same rows; disposing the database drops it. Pass `migrate: true` to run migrations instead of `EnsureCreated`. It implements `IDbContextFactory<TContext>`, so it can stand in for the factory a repository expects.

```csharp
using var database = new SqliteTestDatabase<AppDbContext>(options => new AppDbContext(options));
await using var context = database.CreateContext();
context.Orders.Add(new Order());
await context.SaveChangesAsync();
var repository = new OrderRepository(database);
```

## RecordingHttpHandler

Records every request and its body (null when the request had no content), answers with queued replies in order, then with the responder. With nothing left it throws an `InvalidOperationException` naming the method and URL.

```csharp
var handler = new RecordingHttpHandler().Reply("""{"id":1}""").Reply("{}", HttpStatusCode.NotFound);
handler.Respond(request => new HttpResponseMessage(HttpStatusCode.NoContent));
using var client = handler.CreateClient();
await client.PostAsync("https://api.test/items", new StringContent("hello"));
Assert.Equal("hello", handler.RequestBodies[0]);
```

## TestAuthenticationHandler

Signs a request in as whoever the `Authorization: Test <userId>[;key=value]*` header names. The user id becomes the `NameIdentifier` and `sub` claims, every `key=value` pair becomes a claim. No header gives no result, so an endpoint that requires a user answers 401.

```csharp
builder.Services.AddTestAuthentication();
builder.Services.AddAuthorization();
// later, on the client
var client = app.GetTestClient().AsUser("u1", ("role", "Admin"));
```

## CapturedLogs

A logger provider that keeps every line in memory, every level included. A line reads `{category}: {message}` followed by the state pairs and each active scope's pairs in brackets, for example `Orders: Shipped 7 [OrderId=7] [RequestId=42]`.

```csharp
var logs = new CapturedLogs();
builder.Logging.AddCapturedLogs(logs);
// after the call under test
Assert.Contains(logs.Lines, line => line.StartsWith("Orders: Shipped"));
```

## TestWebApp

Builds a slim `WebApplication` on the in-memory test server, lets the test configure services and the pipeline, and starts it.

```csharp
await using var app = await TestWebApp.StartAsync(
    builder => builder.Services.AddTestAuthentication(),
    webApp => webApp.MapGet("/ping", () => "pong"),
    cancellationToken);
var reply = await app.GetTestClient().GetStringAsync("/ping");
```

## Test framework

The package has no xunit dependency. It works with xunit, NUnit, MSTest or any other test framework.
