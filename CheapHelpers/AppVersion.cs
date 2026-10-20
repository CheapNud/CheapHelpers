using System.Reflection;

namespace CheapHelpers;

/// <summary>
/// The running application's version, read once from the entry assembly's informational version
/// (falling back to the executing assembly), with the build metadata after <c>+</c> split off as the commit hash.
/// </summary>
public static class AppVersion
{
    private const string FallbackVersion = "0.0.0";
    private const int ShortHashLength = 7;

    private static readonly (string Version, string Hash, string Display) Current = Parse(ReadInformationalVersion());

    /// <summary>The version without the <c>+hash</c> suffix, for example <c>1.2.3</c>.</summary>
    public static string Version => Current.Version;

    /// <summary>The build metadata after <c>+</c> (usually the commit hash), or empty.</summary>
    public static string Hash => Current.Hash;

    /// <summary><c>1.2.3 (abc1234)</c> with the hash cut to seven characters, or just the version when there is no hash.</summary>
    public static string Display => Current.Display;

    /// <summary>
    /// Splits an informational version such as <c>1.2.3+abc1234def</c> into version, hash and display text.
    /// A null or blank input gives <c>0.0.0</c>.
    /// </summary>
    public static (string Version, string Hash, string Display) Parse(string? informationalVersion)
    {
        if (string.IsNullOrWhiteSpace(informationalVersion))
            return (FallbackVersion, string.Empty, FallbackVersion);

        var plusIndex = informationalVersion.IndexOf('+');
        var version = (plusIndex < 0 ? informationalVersion : informationalVersion[..plusIndex]).Trim();
        var hash = plusIndex < 0 ? string.Empty : informationalVersion[(plusIndex + 1)..].Trim();

        if (version.Length == 0)
            version = FallbackVersion;

        var display = hash.Length == 0
            ? version
            : $"{version} ({(hash.Length > ShortHashLength ? hash[..ShortHashLength] : hash)})";

        return (version, hash, display);
    }

    private static string? ReadInformationalVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString(3);
    }
}
