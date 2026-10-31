using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

namespace CheapHelpers.TestSupport;

/// <summary>
/// Starts a minimal app on the in-memory test server.
/// </summary>
public static class TestWebApp
{
    /// <summary>Builds a slim app on the test server, lets the caller configure it, and starts it.</summary>
    /// <remarks>
    /// Get a client with <c>app.GetTestClient()</c> and dispose the app when the test ends.
    /// Without <paramref name="environmentName"/> the app reads the usual environment variables and falls back to Production.
    /// </remarks>
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
