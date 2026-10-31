using CheapHelpers.Ota;

namespace CheapHelpers.Tests.Ota;

public class OtaUpdatePolicyTests
{
    private static readonly OtaRelease V100 = new("1.0.0", "v1.0.0", false);
    private static readonly OtaRelease V110Mandatory = new("1.1.0", "v1.1.0", true);
    private static readonly OtaRelease V120 = new("1.2.0", "v1.2.0", false);
    private static readonly OtaRelease V130 = new("1.3.0", "v1.3.0", false);

    private static readonly OtaRelease[] Plain = [V130, V120, V100];
    private static readonly OtaRelease[] WithMandatory = [V130, V120, V110Mandatory, V100];

    [Fact]
    public void Without_a_pin_the_highest_release_wins() =>
        Assert.Equal(V130, OtaUpdatePolicy.ResolveTarget("1.0.0", null, null, Plain));

    [Fact]
    public void A_device_pin_selects_its_release_over_the_instance_pin() =>
        Assert.Equal(V120, OtaUpdatePolicy.ResolveTarget("1.0.0", "1.2.0", "1.3.0", Plain));

    [Fact]
    public void The_instance_pin_applies_when_the_device_has_none() =>
        Assert.Equal(V120, OtaUpdatePolicy.ResolveTarget("1.0.0", " ", "1.2.0", Plain));

    [Fact]
    public void A_pin_without_a_matching_release_gives_null() =>
        Assert.Null(OtaUpdatePolicy.ResolveTarget("1.0.0", "9.9.9", null, Plain));

    [Fact]
    public void A_pin_without_a_matching_release_moves_to_a_mandatory_release() =>
        Assert.Equal(V110Mandatory, OtaUpdatePolicy.ResolveTarget("1.0.0", "9.9.9", null, WithMandatory));

    [Fact]
    public void A_mandatory_release_above_the_pin_lifts_to_the_mandatory_release_not_to_latest() =>
        Assert.Equal(V110Mandatory, OtaUpdatePolicy.ResolveTarget("0.9.0", "1.0.0", null, WithMandatory));

    [Theory]
    [InlineData("1.3.0")]
    [InlineData("2.0.0")]
    public void Nothing_newer_gives_null(string currentVersion) =>
        Assert.Null(OtaUpdatePolicy.ResolveTarget(currentVersion, null, null, Plain));

    [Fact]
    public void A_leading_v_is_accepted_on_the_current_version_and_the_pin()
    {
        Assert.Null(OtaUpdatePolicy.ResolveTarget("v1.3.0", null, null, Plain));
        Assert.Equal(V120, OtaUpdatePolicy.ResolveTarget("v1.0.0", "v1.2.0", null, Plain));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("garbage")]
    public void An_unparsable_current_version_counts_as_zero(string? currentVersion) =>
        Assert.Equal(V130, OtaUpdatePolicy.ResolveTarget(currentVersion, null, null, Plain));

    [Fact]
    public void IsMandatoryAbove_sees_a_mandatory_release_between_current_and_latest()
    {
        Assert.True(OtaUpdatePolicy.IsMandatoryAbove("1.0.0", WithMandatory));
        Assert.False(OtaUpdatePolicy.IsMandatoryAbove("1.1.0", WithMandatory));
    }

    [Fact]
    public void IsMandatoryAbove_is_false_without_mandatory_releases() =>
        Assert.False(OtaUpdatePolicy.IsMandatoryAbove("1.0.0", Plain));

    [Fact]
    public void IsMandatoryAbove_with_an_unparsable_current_version_reads_only_the_newest_release()
    {
        Assert.False(OtaUpdatePolicy.IsMandatoryAbove(null, WithMandatory));
        Assert.True(OtaUpdatePolicy.IsMandatoryAbove("garbage", [V110Mandatory, V100]));
        Assert.False(OtaUpdatePolicy.IsMandatoryAbove(null, []));
    }
}
