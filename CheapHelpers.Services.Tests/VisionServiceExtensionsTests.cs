using CheapHelpers.Services.Vision;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CheapHelpers.Services.Tests;

// A plain ServiceCollection with IStartupValidator (Microsoft.Extensions.Options) stands in for host start,
// since the test project does not reference Microsoft.Extensions.Hosting.
public class VisionServiceExtensionsTests
{
    private const string Endpoint = "https://vision.example.test/";
    private const string ApiKey = "test-key";

    [Fact]
    public void A_configured_section_resolves_the_azure_service()
    {
        using var provider = BuildProvider(new()
        {
            [$"{VisionServiceOptions.SectionName}:Endpoint"] = Endpoint,
            [$"{VisionServiceOptions.SectionName}:ApiKey"] = ApiKey,
        });

        provider.GetRequiredService<IStartupValidator>().Validate();

        Assert.IsType<AzureVisionService>(provider.GetRequiredService<IVisionService>());
    }

    [Fact]
    public void A_missing_key_fails_validation_at_startup()
    {
        using var provider = BuildProvider(new()
        {
            [$"{VisionServiceOptions.SectionName}:Endpoint"] = Endpoint,
        });

        var error = Assert.ThrowsAny<Exception>(() => provider.GetRequiredService<IStartupValidator>().Validate());

        Assert.Contains("AzureVision:ApiKey", error.Message);
    }

    [Fact]
    public void The_delegate_overload_registers_the_same_singleton()
    {
        using var provider = new ServiceCollection()
            .AddAzureVision(o =>
            {
                o.Endpoint = Endpoint;
                o.ApiKey = ApiKey;
            })
            .BuildServiceProvider();

        provider.GetRequiredService<IStartupValidator>().Validate();

        Assert.Same(provider.GetRequiredService<IVisionService>(), provider.GetRequiredService<IVisionService>());
    }

    private static ServiceProvider BuildProvider(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new ServiceCollection().AddAzureVision(configuration).BuildServiceProvider();
    }
}
