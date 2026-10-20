using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.Auth.Tests;

public class DataProtectionTests
{
    private const UnixFileMode OwnerOnly = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;

    [Fact]
    public void Creates_the_key_folder_and_round_trips_a_string()
    {
        var keysPath = NewKeysPath();
        try
        {
            var services = new ServiceCollection();
            services.AddCheapDataProtection(ConfigurationFor(keysPath), "auth-tests");

            Assert.True(Directory.Exists(keysPath));

            using var serviceProvider = services.BuildServiceProvider();
            var protector = serviceProvider.GetRequiredService<IDataProtectionProvider>().CreateProtector("tests");
            var protectedText = protector.Protect("plain text");

            Assert.Equal("plain text", protector.Unprotect(protectedText));
            Assert.NotEmpty(Directory.GetFiles(keysPath, "key-*.xml"));
        }
        finally
        {
            Cleanup(keysPath);
        }
    }

    [Fact]
    public void A_configured_path_stores_keys_without_dpapi_so_a_shared_ring_keeps_working()
    {
        var keysPath = NewKeysPath();
        try
        {
            var services = new ServiceCollection();
            services.AddCheapDataProtection(ConfigurationFor(keysPath), "auth-tests");

            using var serviceProvider = services.BuildServiceProvider();
            serviceProvider.GetRequiredService<IDataProtectionProvider>().CreateProtector("tests").Protect("plain text");

            var keyXml = File.ReadAllText(Directory.GetFiles(keysPath, "key-*.xml").Single());

            // DPAPI would wrap the master key in an encryptedKey element; a configured path must stay portable
            Assert.DoesNotContain("encryptedKey", keyXml);
        }
        finally
        {
            Cleanup(keysPath);
        }
    }

    [Fact]
    public void A_folder_this_code_creates_is_owner_only_on_unix()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Unix file modes do not apply on Windows");
        if (OperatingSystem.IsWindows())
            return; // the platform analyzer does not see through Assert.SkipWhen

        var keysPath = NewKeysPath();
        try
        {
            var services = new ServiceCollection();
            services.AddCheapDataProtection(ConfigurationFor(keysPath), "auth-tests");

            Assert.Equal(OwnerOnly, File.GetUnixFileMode(keysPath));
        }
        finally
        {
            Cleanup(keysPath);
        }
    }

    [Fact]
    public void An_existing_folder_keeps_its_mode()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Unix file modes do not apply on Windows");
        if (OperatingSystem.IsWindows())
            return; // the platform analyzer does not see through Assert.SkipWhen

        var keysPath = NewKeysPath();
        try
        {
            var shared = OwnerOnly | UnixFileMode.GroupRead | UnixFileMode.GroupExecute;
            Directory.CreateDirectory(keysPath, shared);

            var services = new ServiceCollection();
            services.AddCheapDataProtection(ConfigurationFor(keysPath), "auth-tests");

            Assert.Equal(shared, File.GetUnixFileMode(keysPath));
        }
        finally
        {
            Cleanup(keysPath);
        }
    }

    [Fact]
    public void Default_key_path_is_outside_the_app_folder_when_local_application_data_resolves()
    {
        var localApplicationData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify);
        Assert.SkipWhen(string.IsNullOrEmpty(localApplicationData), "LocalApplicationData does not resolve on this host");

        var defaultPath = DataProtectionExtensions.DefaultKeysPath("auth-tests");

        Assert.EndsWith(Path.Combine("auth-tests", "keys"), defaultPath);
        Assert.False(defaultPath.StartsWith(AppContext.BaseDirectory, StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("https://example.com", "https___example.com")]
    [InlineData("..\\..\\escape", ".._.._escape")]
    [InlineData("..", "app")]
    [InlineData("/", "app")]
    [InlineData("My App", "My App")]
    public void Application_names_map_to_one_safe_folder_component(string applicationName, string expected)
    {
        Assert.Equal(expected, DataProtectionExtensions.SafeFolderName(applicationName));
        Assert.DoesNotContain(Path.DirectorySeparatorChar, DataProtectionExtensions.SafeFolderName(applicationName));
    }

    private static string NewKeysPath() =>
        Path.Combine(Path.GetTempPath(), $"cheaphelpers-auth-keys-{Guid.NewGuid():N}");

    private static IConfiguration ConfigurationFor(string keysPath) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DataProtection:KeysPath"] = keysPath })
            .Build();

    private static void Cleanup(string keysPath)
    {
        if (Directory.Exists(keysPath))
            Directory.Delete(keysPath, recursive: true);
    }
}
