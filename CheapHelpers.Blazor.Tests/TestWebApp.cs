using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

namespace CheapHelpers.Blazor.Tests;

/// <summary>
/// Starts a minimal app on the in-memory test server.
/// </summary>
internal static class TestWebApp
{
    /// <summary>Builds a slim app on the test server, lets the caller configure it, and starts it.</summary>
    public static async Task<WebApplication> StartAsync(
        Action<WebApplicationBuilder> configureBuilder,
        Action<WebApplication> configureApp,
        CancellationToken cancellationToken,
        string? environmentName = null)
    {
        // The environment has to be set here; changing builder.Environment later does not reach the built app
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { EnvironmentName = environmentName });
        builder.WebHost.UseTestServer();
        configureBuilder(builder);

        var app = builder.Build();
        configureApp(app);
        await app.StartAsync(cancellationToken);
        return app;
    }
}
