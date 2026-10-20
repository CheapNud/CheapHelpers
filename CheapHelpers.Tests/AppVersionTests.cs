namespace CheapHelpers.Tests;

/// <summary>Pins how an informational version splits into version, hash and display text.</summary>
public sealed class AppVersionTests
{
    [Fact]
    public void Parse_splits_off_the_hash_and_shortens_it_for_display()
    {
        var (version, hash, display) = AppVersion.Parse("1.2.3+abc1234def5678");

        Assert.Equal("1.2.3", version);
        Assert.Equal("abc1234def5678", hash);
        Assert.Equal("1.2.3 (abc1234)", display);
    }

    [Fact]
    public void Parse_without_a_hash_displays_just_the_version()
    {
        Assert.Equal(("4.0.0-beta.1", "", "4.0.0-beta.1"), AppVersion.Parse("4.0.0-beta.1"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Parse_falls_back_to_zero_for_a_missing_version(string? informationalVersion)
    {
        Assert.Equal(("0.0.0", "", "0.0.0"), AppVersion.Parse(informationalVersion));
    }

    [Fact]
    public void Current_version_is_read_without_its_hash()
    {
        Assert.False(string.IsNullOrWhiteSpace(AppVersion.Version));
        Assert.DoesNotContain('+', AppVersion.Version);
        Assert.StartsWith(AppVersion.Version, AppVersion.Display);
    }
}
