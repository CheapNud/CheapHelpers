using System.Net;
using System.Text;
using CheapHelpers.Auth.Ota;
using CheapHelpers.Ota;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CheapHelpers.Auth.Tests;

public class OtaEndpointTests
{
    private const string Prefix = "/api/ota";
    private const string Binary = "agent-armv7";

    private static readonly OtaRelease[] Releases =
    [
        new("1.2.0", "v1.2.0", false),
        new("1.1.0", "v1.1.0", true),
        new("1.0.0", "v1.0.0", false),
    ];

    [Fact]
    public async Task The_release_list_has_exact_field_names_newest_first()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await StartAsync(new FakeReleaseSource(Releases), cancellationToken);

        var body = await app.GetTestClient().GetStringAsync($"{Prefix}/releases", cancellationToken);

        Assert.Equal(
            """[{"version":"1.2.0","tag":"v1.2.0","mandatory":false},{"version":"1.1.0","tag":"v1.1.0","mandatory":true},{"version":"1.0.0","tag":"v1.0.0","mandatory":false}]""",
            body);
    }

    [Fact]
    public async Task Latest_answers_the_newest_release_with_both_paths_and_mandatory_since_the_given_version()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await StartAsync(new FakeReleaseSource(Releases), cancellationToken);

        var body = await app.GetTestClient().GetStringAsync($"{Prefix}/releases/latest?arch=ARMV7&since=1.0.0", cancellationToken);

        Assert.Equal(
            """{"version":"1.2.0","tag":"v1.2.0","mandatory":true,"download_url":"/api/ota/releases/1.2.0/agent-armv7","sha256_url":"/api/ota/releases/1.2.0/agent-armv7.sha256"}""",
            body);
    }

    [Fact]
    public async Task Latest_links_carry_a_parent_group_prefix()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await StartAsync(new FakeReleaseSource(Releases), cancellationToken, webApp => webApp.MapGroup("/edge").MapCheapOtaReleaseEndpoints());

        var body = await app.GetTestClient().GetStringAsync($"/edge{Prefix}/releases/latest?arch=armv7", cancellationToken);

        Assert.Contains("\"download_url\":\"/edge/api/ota/releases/1.2.0/agent-armv7\"", body);
    }

    [Fact]
    public async Task Latest_answers_404_without_releases_and_400_for_an_unknown_or_missing_architecture()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var empty = await StartAsync(new FakeReleaseSource([]), cancellationToken);
        await using var filled = await StartAsync(new FakeReleaseSource(Releases), cancellationToken);

        using var noRelease = await empty.GetTestClient().GetAsync($"{Prefix}/releases/latest?arch=armv7", cancellationToken);
        using var unknownArch = await filled.GetTestClient().GetAsync($"{Prefix}/releases/latest?arch=x86_64", cancellationToken);
        using var missingArch = await filled.GetTestClient().GetAsync($"{Prefix}/releases/latest", cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, noRelease.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, unknownArch.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missingArch.StatusCode);
    }

    [Fact]
    public async Task An_asset_streams_as_a_download_and_a_missing_one_is_404()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var source = new FakeReleaseSource(Releases) { Assets = { [("1.2.0", Binary)] = "binary-bytes" } };
        await using var app = await StartAsync(source, cancellationToken);
        var client = app.GetTestClient();

        using var found = await client.GetAsync($"{Prefix}/releases/1.2.0/{Binary}", cancellationToken);
        using var missing = await client.GetAsync($"{Prefix}/releases/1.2.0/{Binary}.sha256", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, found.StatusCode);
        Assert.Equal("application/octet-stream", found.Content.Headers.ContentType?.MediaType);
        Assert.Equal(Binary, found.Content.Headers.ContentDisposition?.FileName);
        Assert.Equal("binary-bytes", await found.Content.ReadAsStringAsync(cancellationToken));
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task An_unknown_asset_name_is_400_and_never_reaches_the_source()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var source = new FakeReleaseSource(Releases);
        await using var app = await StartAsync(source, cancellationToken);

        using var response = await app.GetTestClient().GetAsync($"{Prefix}/releases/1.2.0/install.sh", cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, source.AssetCalls);
    }

    [Theory]
    [InlineData("/releases")]
    [InlineData("/releases/latest?arch=armv7")]
    [InlineData("/releases/1.2.0/agent-armv7")]
    public async Task A_failing_source_gives_503(string path)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await StartAsync(new FakeReleaseSource(Releases) { Fail = true }, cancellationToken);

        using var response = await app.GetTestClient().GetAsync(Prefix + path, cancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Release source unavailable", await response.Content.ReadAsStringAsync(cancellationToken));
    }

    [Fact]
    public async Task CheckAsync_builds_both_paths_under_the_given_prefix()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var checker = new OtaUpdateChecker(new FakeReleaseSource(Releases), Options.Create(CreateOptions()));

        var check = await checker.CheckAsync("1.0.0", "armv7", null, "/devices/ota/", cancellationToken);

        Assert.Equal(new OtaUpdateCheck("1.2.0", "v1.2.0", true, "/devices/ota/releases/1.2.0/agent-armv7", "/devices/ota/releases/1.2.0/agent-armv7.sha256"), check);
    }

    [Fact]
    public async Task CheckAsync_returns_null_when_nothing_is_newer_and_throws_for_an_unknown_architecture()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var checker = new OtaUpdateChecker(new FakeReleaseSource(Releases), Options.Create(CreateOptions()));

        Assert.Null(await checker.CheckAsync("1.2.0", "armv7", null, Prefix, cancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => checker.CheckAsync("1.0.0", "x86_64", null, Prefix, cancellationToken));
    }

    [Fact]
    public async Task AddCheapOta_fails_at_startup_when_the_source_setting_is_missing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => TestWebApp.StartAsync(
            builder => builder.Services.AddCheapOta(otaOptions => otaOptions.Assets["armv7"] = Binary),
            webApp => webApp.MapCheapOtaReleaseEndpoints(),
            cancellationToken));

        Assert.Contains(nameof(OtaOptions.ForgeRepoApiUrl), error.Message);
    }

    [Fact]
    public async Task UpstreamReleaseSource_round_trips_the_list_and_an_asset_through_another_instance()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var upstreamSource = new FakeReleaseSource(Releases) { Assets = { [("1.2.0", Binary)] = "binary-bytes" } };
        await using var upstream = await StartAsync(upstreamSource, cancellationToken);
        var source = new UpstreamReleaseSource(
            upstream.GetTestClient(),
            Options.Create(new OtaOptions { Source = OtaSourceKind.Upstream, UpstreamUrl = $"http://localhost{Prefix}/", Assets = { ["armv7"] = Binary } }));

        var releases = await source.GetReleasesAsync(cancellationToken);
        await using var assetStream = await source.OpenAssetAsync("1.2.0", Binary, cancellationToken);
        var missing = await source.OpenAssetAsync("9.9.9", Binary, cancellationToken);

        Assert.Equal(Releases, releases);
        Assert.NotNull(assetStream);
        using var reader = new StreamReader(assetStream);
        Assert.Equal("binary-bytes", await reader.ReadToEndAsync(cancellationToken));
        Assert.Null(missing);
    }

    private static OtaOptions CreateOptions() => new()
    {
        Source = OtaSourceKind.Upstream,
        UpstreamUrl = "http://unused.test/api/ota",
        Assets = { ["armv7"] = Binary, ["aarch64"] = "agent-aarch64" },
    };

    private static Task<WebApplication> StartAsync(FakeReleaseSource source, CancellationToken cancellationToken, Action<WebApplication>? mapEndpoints = null) =>
        TestWebApp.StartAsync(
            builder =>
            {
                var defaults = CreateOptions();
                builder.Services.AddCheapOta(otaOptions =>
                {
                    otaOptions.Source = defaults.Source;
                    otaOptions.UpstreamUrl = defaults.UpstreamUrl;
                    foreach (var asset in defaults.Assets)
                        otaOptions.Assets[asset.Key] = asset.Value;
                });
                builder.Services.AddSingleton<IOtaReleaseSource>(source);
            },
            mapEndpoints ?? (webApp => webApp.MapCheapOtaReleaseEndpoints()),
            cancellationToken);

    /// <summary>Serves fixed releases and in-memory assets, or throws as an unreachable forge would.</summary>
    private sealed class FakeReleaseSource(IReadOnlyList<OtaRelease> releases) : IOtaReleaseSource
    {
        public Dictionary<(string Version, string AssetName), string> Assets { get; } = [];

        public bool Fail { get; init; }

        public int AssetCalls { get; private set; }

        public async Task<IReadOnlyList<OtaRelease>> GetReleasesAsync(CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return Fail ? throw new HttpRequestException("forge down") : releases;
        }

        public async Task<Stream?> OpenAssetAsync(string version, string assetName, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            AssetCalls++;
            if (Fail)
                throw new HttpRequestException("forge down");

            return Assets.TryGetValue((version, assetName), out var content) ? new MemoryStream(Encoding.UTF8.GetBytes(content)) : null;
        }
    }
}
