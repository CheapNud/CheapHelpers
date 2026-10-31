using CheapHelpers.Ota;

namespace CheapHelpers.Tests.Ota;

public class OtaOptionsTests
{
    [Fact]
    public void Validate_names_Assets_when_it_is_empty()
    {
        var otaOptions = new OtaOptions { ForgeRepoApiUrl = "https://forge.test/api/v1/repos/o/r" };

        var error = Assert.Throws<InvalidOperationException>(otaOptions.Validate);

        Assert.Contains(nameof(OtaOptions.Assets), error.Message);
    }

    [Theory]
    [InlineData(OtaSourceKind.Forge, nameof(OtaOptions.ForgeRepoApiUrl))]
    [InlineData(OtaSourceKind.Upstream, nameof(OtaOptions.UpstreamUrl))]
    [InlineData(OtaSourceKind.LocalFolder, nameof(OtaOptions.LocalPath))]
    public void Validate_names_the_setting_the_source_needs(OtaSourceKind source, string settingName)
    {
        var otaOptions = new OtaOptions { Source = source, Assets = { ["armv7"] = "agent-armv7" }, LocalPath = source == OtaSourceKind.LocalFolder ? " " : null };

        var error = Assert.Throws<InvalidOperationException>(otaOptions.Validate);

        Assert.Contains(settingName, error.Message);
    }

    [Fact]
    public void Validate_passes_with_assets_and_the_source_setting()
    {
        var otaOptions = new OtaOptions { Source = OtaSourceKind.Upstream, UpstreamUrl = "https://up.test/api/ota", Assets = { ["armv7"] = "agent-armv7" } };

        otaOptions.Validate();
    }

    [Theory]
    [InlineData("agent-armv7", true)]
    [InlineData("agent-armv7.sha256", true)]
    [InlineData("agent-aarch64.sha256", true)]
    [InlineData("AGENT-ARMV7", false)]
    [InlineData("agent-armv7.sig", false)]
    [InlineData("../agent-armv7", false)]
    [InlineData("install.sh", false)]
    public void IsKnownAsset_accepts_only_the_binaries_and_their_checksums(string assetName, bool known)
    {
        var otaOptions = new OtaOptions { Assets = { ["armv7"] = "agent-armv7", ["aarch64"] = "agent-aarch64" } };

        Assert.Equal(known, otaOptions.IsKnownAsset(assetName));
    }

    [Fact]
    public void AssetFor_matches_the_architecture_case_insensitively()
    {
        var otaOptions = new OtaOptions { Assets = { ["armv7"] = "agent-armv7" } };

        Assert.Equal("agent-armv7", otaOptions.AssetFor("ARMv7"));
        Assert.Null(otaOptions.AssetFor("x86_64"));
    }
}
