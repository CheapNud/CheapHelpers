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
        var keysPath = Path.Combine(Path.GetTempPath(), $"cheaphelpers-auth-keys-{Guid.NewGuid():N}");
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["DataProtection:KeysPath"] = keysPath })
                .Build();

            var services = new ServiceCollection();
            services.AddCheapDataProtection(configuration, "auth-tests");

            Assert.True(Directory.Exists(keysPath));

            using var serviceProvider = services.BuildServiceProvider();
            var protector = serviceProvider.GetRequiredService<IDataProtectionProvider>().CreateProtector("tests");
            var protectedText = protector.Protect("plain text");

            Assert.Equal("plain text", protector.Unprotect(protectedText));
            Assert.NotEmpty(Directory.GetFiles(keysPath, "key-*.xml"));
        }
        finally
        {
            if (Directory.Exists(keysPath))
                Directory.Delete(keysPath, recursive: true);
        }
    }

    [Fact]
    public void Keys_are_dpapi_encrypted_on_windows_and_the_folder_is_owner_only_elsewhere()
    {
        var keysPath = Path.Combine(Path.GetTempPath(), $"cheaphelpers-auth-keys-{Guid.NewGuid():N}");
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["DataProtection:KeysPath"] = keysPath })
                .Build();

            var services = new ServiceCollection();
            services.AddCheapDataProtection(configuration, "auth-tests");

            using var serviceProvider = services.BuildServiceProvider();
            serviceProvider.GetRequiredService<IDataProtectionProvider>().CreateProtector("tests").Protect("plain text");

            var keyXml = File.ReadAllText(Directory.GetFiles(keysPath, "key-*.xml").Single());

            if (OperatingSystem.IsWindows())
            {
                // DPAPI wraps the master key in an encryptedKey element instead of writing it in the clear
                Assert.Contains("encryptedKey", keyXml);
            }
            else
            {
                Assert.Equal(OwnerOnly, File.GetUnixFileMode(keysPath));
            }
        }
        finally
        {
            if (Directory.Exists(keysPath))
                Directory.Delete(keysPath, recursive: true);
        }
    }

    [Fact]
    public void Default_key_path_is_outside_the_app_folder_when_local_application_data_exists()
    {
        var defaultPath = DataProtectionExtensions.DefaultKeysPath("auth-tests");

        Assert.EndsWith(Path.Combine("auth-tests", "keys"), defaultPath);
        Assert.False(defaultPath.StartsWith(AppContext.BaseDirectory, StringComparison.OrdinalIgnoreCase));
    }
}
