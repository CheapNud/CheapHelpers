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

    // Owner read, write and traverse only: the key ring signs every auth cookie, so nobody else on the host may read it
    private const UnixFileMode OwnerOnly = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;

    /// <summary>
    /// Persists the Data Protection key ring to <c>{sectionKey}:KeysPath</c>, or to
    /// <c>{LocalApplicationData}/{applicationName}/keys</c> when that is not set, and creates the folder readable
    /// by the app's user only. Auth cookies and antiforgery tokens then survive a restart or redeploy.
    /// On Windows the key files are encrypted with DPAPI for the account the app runs as; elsewhere they are
    /// plain XML, so the folder permissions are the only protection and the folder must not sit on a shared mount.
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
            keysPath = DefaultKeysPath(applicationName);

        var keysDirectory = CreateOwnerOnlyDirectory(keysPath);

        var dataProtection = services.AddDataProtection()
            .PersistKeysToFileSystem(keysDirectory)
            .SetApplicationName(applicationName);

        if (OperatingSystem.IsWindows())
            dataProtection.ProtectKeysWithDpapi();

        services.AddSingleton<IStartupFilter>(new StartupLogFilter(LogLevel.Information,
            "Data Protection keys for {ApplicationName} are stored in {KeysPath}", applicationName, keysDirectory.FullName));

        return services;
    }

    /// <summary>
    /// The default key folder lives under the user's local application data, not next to the app,
    /// so a redeploy that replaces the app folder does not wipe the keys and sign everyone out.
    /// </summary>
    internal static string DefaultKeysPath(string applicationName)
    {
        // DoNotVerify: a fresh service account has no ~/.local/share yet, and the default option would then return
        // an empty string; the folder is created below anyway
        var localApplicationData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify);

        // A container without HOME still resolves LocalApplicationData to an empty string; fall back to the app folder
        var root = string.IsNullOrEmpty(localApplicationData)
            ? AppContext.BaseDirectory
            : Path.Combine(localApplicationData, applicationName);

        return Path.Combine(root, DefaultKeysFolder);
    }

    private static DirectoryInfo CreateOwnerOnlyDirectory(string path)
    {
        if (OperatingSystem.IsWindows())
            return Directory.CreateDirectory(path);

        var keysDirectory = Directory.CreateDirectory(path, OwnerOnly);

        // CreateDirectory leaves the mode of an existing folder alone, so tighten it on every start
        File.SetUnixFileMode(path, OwnerOnly);
        return keysDirectory;
    }
}
