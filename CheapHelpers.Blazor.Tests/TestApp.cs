using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

namespace CheapHelpers.Blazor.Tests;

/// <summary>
/// Starts a minimal app on the in-memory test server.
/// </summary>
internal static class TestApp
{
    public static async Task<WebApplication> StartAsync(
        Action<WebApplicationBuilder> configureBuilder,
        Action<WebApplication> configureApp,
        CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        configureBuilder(builder);

        var app = builder.Build();
        configureApp(app);
        await app.StartAsync(cancellationToken);
        return app;
    }
}
