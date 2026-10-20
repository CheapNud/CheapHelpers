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
    private const UnixFileMode GroupOrOthers = UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute
        | UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute;

    /// <summary>
    /// Persists the Data Protection key ring to <c>{sectionKey}:KeysPath</c>, or to
    /// <c>{LocalApplicationData}/{applicationName}/keys</c> when that is not set. Auth cookies and antiforgery
    /// tokens then survive a restart or redeploy. A folder this method creates is readable by the app's user only;
    /// an existing folder keeps its permissions and a warning is logged when others can read it.
    /// With the default path the key files are encrypted with DPAPI on Windows; with a configured path they are
    /// stored as the app writes them, so a key ring shared between nodes or accounts keeps working. Add
    /// <c>ProtectKeysWithCertificate</c> or <c>ProtectKeysWithDpapi</c> on the returned builder when a configured
    /// path needs encryption at rest.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Configuration holding the Data Protection section.</param>
    /// <param name="applicationName">Name that isolates this app's keys; apps sharing a key folder need the same name to read each other's cookies.</param>
    /// <param name="sectionKey">Section that holds <c>KeysPath</c>.</param>
    /// <returns>The Data Protection builder, for further key protection options.</returns>
    public static IDataProtectionBuilder AddCheapDataProtection(
        this IServiceCollection services,
        IConfiguration configuration,
        string applicationName,
        string sectionKey = "DataProtection")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionKey);

        var configuredPath = configuration.GetSection(sectionKey)[KeysPathKey];
        var usesDefaultPath = string.IsNullOrWhiteSpace(configuredPath);
        var keysPath = usesDefaultPath ? DefaultKeysPath(applicationName) : configuredPath!;

        var (keysDirectory, othersCanRead) = CreateKeysDirectory(keysPath);

        var dataProtection = services.AddDataProtection()
            .PersistKeysToFileSystem(keysDirectory)
            .SetApplicationName(applicationName);

        // Only for the local default folder: DPAPI binds the keys to this account on this machine, which would
        // break a configured folder shared between nodes or a service that changes account
        if (usesDefaultPath && OperatingSystem.IsWindows())
            dataProtection.ProtectKeysWithDpapi();

        services.AddSingleton<IStartupFilter>(new StartupLogFilter(LogLevel.Information,
            "Data Protection keys for {ApplicationName} are stored in {KeysPath}", applicationName, keysDirectory.FullName));

        if (othersCanRead)
        {
            services.AddSingleton<IStartupFilter>(new StartupLogFilter(LogLevel.Warning,
                "The Data Protection key folder {KeysPath} is readable by other users; restrict it to the app's user", keysDirectory.FullName));
        }

        return dataProtection;
    }

    /// <summary>
    /// The default key folder lives under the user's local application data, not next to the app,
    /// so a redeploy that replaces the app folder does not wipe the keys and sign everyone out.
    /// </summary>
    internal static string DefaultKeysPath(string applicationName)
    {
        // DoNotVerify: a fresh service account has no ~/.local/share yet, and the default option would then return
        // an empty string; the folder is created later anyway
        var localApplicationData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify);

        // A container without HOME still resolves LocalApplicationData to an empty string; fall back to the app folder
        var root = string.IsNullOrEmpty(localApplicationData)
            ? AppContext.BaseDirectory
            : Path.Combine(localApplicationData, SafeFolderName(applicationName));

        return Path.Combine(root, DefaultKeysFolder);
    }

    /// <summary>
    /// A Data Protection application name may be any string, such as a URL; this maps it to one folder component
    /// so it can neither fail on invalid path characters nor escape the parent folder.
    /// </summary>
    internal static string SafeFolderName(string applicationName)
    {
        var invalid = Path.GetInvalidFileNameChars().Concat(['/', '\\', ':']).Distinct().ToArray();
        var safe = string.Join('_', applicationName.Split(invalid)).Trim();

        // Names made only of separators or dots ("/", "..") would become "_" or a traversal; use a plain folder instead
        return safe.Trim('_', '.').Length == 0 ? DefaultAppFolder : safe;
    }

    private const string DefaultAppFolder = "app";

    /// <summary>
    /// Creates the folder owner-only when it does not exist. An existing folder keeps its mode, because it may be
    /// a shared or mounted volume the app does not own; the caller is told when others can read it.
    /// </summary>
    private static (DirectoryInfo Directory, bool OthersCanRead) CreateKeysDirectory(string path)
    {
        if (OperatingSystem.IsWindows())
            return (Directory.CreateDirectory(path), false);

        if (!Directory.Exists(path))
            return (Directory.CreateDirectory(path, OwnerOnly), false);

        var othersCanRead = (File.GetUnixFileMode(path) & GroupOrOthers) != 0;
        return (new DirectoryInfo(path), othersCanRead);
    }
}
