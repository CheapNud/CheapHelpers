using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Auth;

/// <summary>
/// Registers ASP.NET Core Data Protection with a key ring persisted to disk.
/// </summary>
public static class DataProtectionExtensions
{
    private const string KeysPathKey = "KeysPath";
    private const string DefaultKeysFolder = "keys";

    /// <summary>
    /// Persists the Data Protection key ring to <c>{sectionKey}:KeysPath</c>, or to a <c>keys</c> folder next to
    /// the app when that is not set, and creates the folder. Auth cookies and antiforgery tokens then survive
    /// a restart or redeploy. The key files are written unencrypted: this does not encrypt secrets at rest,
    /// so keep the folder readable only by the app.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Configuration holding the Data Protection section.</param>
    /// <param name="applicationName">Name that isolates this app's keys; apps sharing a key folder need the same name to read each other's cookies.</param>
    /// <param name="sectionKey">Section that holds <c>KeysPath</c>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCheapDataProtection(
        this IServiceCollection services,
        IConfiguration configuration,
        string applicationName,
        string sectionKey = "DataProtection")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionKey);

        var keysPath = configuration.GetSection(sectionKey)[KeysPathKey];
        if (string.IsNullOrWhiteSpace(keysPath))
            keysPath = Path.Combine(AppContext.BaseDirectory, DefaultKeysFolder);

        var keysDirectory = Directory.CreateDirectory(keysPath);

        services.AddDataProtection()
            .PersistKeysToFileSystem(keysDirectory)
            .SetApplicationName(applicationName);

        services.AddSingleton<IStartupFilter>(new StartupLogFilter(LogLevel.Information,
            "Data Protection keys for {ApplicationName} are stored in {KeysPath}", applicationName, keysDirectory.FullName));

        return services;
    }
}
