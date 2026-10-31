# Test doubles

Two helpers for tests of code that talks to HTTP or writes logs, in the `CheapHelpers.Testing` namespace; neither depends on a test framework.

## RecordingHttpHandler

Records every request and its body (null when the request had no content), answers with queued replies in order, then with the responder. With nothing left it throws an `InvalidOperationException` naming the method and URL.

```csharp
var handler = new RecordingHttpHandler().Reply("""{"id":1}""").Reply("{}", HttpStatusCode.NotFound);
handler.Respond(request => new HttpResponseMessage(HttpStatusCode.NoContent));
using var client = handler.CreateClient();
await client.PostAsync("https://api.test/items", new StringContent("hello"));
Assert.Equal("hello", handler.RequestBodies[0]);
```

## CapturedLogs

A logger provider that keeps every line in memory, every level included. A line reads `{category}: {message}` followed by the state pairs and each active scope's pairs in brackets, for example `Orders: Shipped 7 [OrderId=7] [RequestId=42]`.

```csharp
var logs = new CapturedLogs();
builder.Logging.AddCapturedLogs(logs);
// after the call under test
Assert.Contains(logs.Lines, line => line.StartsWith("Orders: Shipped"));
```
