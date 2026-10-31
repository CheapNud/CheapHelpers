using Microsoft.Extensions.Logging;

namespace CheapHelpers.Tests;

public class CapturedLogsTests
{
    private const string Category = "Orders";

    [Fact]
    public void Captures_trace_with_state_and_scope_pairs()
    {
        var logs = new CapturedLogs();
        using (var factory = LoggerFactory.Create(logging => logging.AddCapturedLogs(logs)))
        {
            var logger = factory.CreateLogger(Category);
            using (logger.BeginScope(new Dictionary<string, object?> { ["RequestId"] = 42 }))
            {
                logger.LogTrace("Shipped {OrderId}", 7);
            }
        }

        Assert.Equal(["Orders: Shipped 7 [OrderId=7] [RequestId=42]"], logs.Lines);
    }
}
