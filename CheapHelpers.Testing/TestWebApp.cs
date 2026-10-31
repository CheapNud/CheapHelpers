using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

namespace CheapHelpers.Testing;

/// <summary>
/// Starts a minimal app on the in-memory test server.
/// </summary>
public static class TestWebApp
{
    /// <summary>Builds a slim app on the test server, lets the caller configure it, and starts it.</summary>
    /// <remarks>Get a client with <c>app.GetTestClient()</c> and dispose the app when the test ends.</remarks>
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
