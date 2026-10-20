using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Auth;

/// <summary>
/// Writes one log entry when the app pipeline is built. Service registration runs before logging exists,
/// so registration helpers use this to report what they set up.
/// </summary>
internal sealed class StartupLogFilter(LogLevel level, string message, params object?[] args) : IStartupFilter
{
    private const string LoggerCategory = "CheapHelpers.Auth";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.ApplicationServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger(LoggerCategory)
            .Log(level, message, args);
        next(app);
    };
}
