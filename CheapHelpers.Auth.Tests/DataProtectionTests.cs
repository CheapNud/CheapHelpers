using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.Auth.Tests;

public class DataProtectionTests
{
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
}
