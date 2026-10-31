using System.Net;
using System.Text.Json;
using CheapHelpers.Ota;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CheapHelpers.Tests.Ota;

public class ForgeReleaseSourceTests
{
    private const string RepoApiUrl = "http://forge.test/api/v1/repos/owner/agent";
    private const string Binary = "agent-armv7";
    private const string DownloadUrl = "http://forge.test/owner/agent/releases/download/v1.2.0/agent-armv7";

    [Fact]
    public async Task Pages_until_a_short_page_and_returns_one_list_newest_first()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHttpHandler()
            .Reply(ReleasesJson("v1.0.0", "v1.3.0"))
            .Reply(ReleasesJson("v1.10.0", "v1.2.0"))
            .Reply(ReleasesJson("v0.9.0"));
        var source = CreateSource(handler, pageSize: 2);

        var releases = await source.GetReleasesAsync(cancellationToken);

        Assert.Equal(["1.10.0", "1.3.0", "1.2.0", "1.0.0", "0.9.0"], releases.Select(release => release.Version));
        Assert.Equal(
            [$"{RepoApiUrl}/releases?limit=2&page=1", $"{RepoApiUrl}/releases?limit=2&page=2", $"{RepoApiUrl}/releases?limit=2&page=3"],
            handler.Requests.Select(request => request.RequestUri!.ToString()));
    }

    [Fact]
    public async Task Drops_tags_without_the_prefix_and_versions_that_do_not_parse()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHttpHandler().Reply(ReleasesJson("v1.0.0", "1.1.0", "docs-1.2.0", "vnext", "v1.3"));
        var source = CreateSource(handler);

        var releases = await source.GetReleasesAsync(cancellationToken);

        Assert.Equal(["v1.3", "v1.0.0"], releases.Select(release => release.Tag));
    }

    [Fact]
    public async Task Reads_the_mandatory_marker_case_insensitively()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHttpHandler().Reply(JsonSerializer.Serialize(new[]
        {
            new { tag_name = "v1.1.0", body = "Fixes the watchdog. [MANDATORY]" },
            new { tag_name = "v1.0.0", body = "Plain release" },
        }));
        var source = CreateSource(handler);

        var releases = await source.GetReleasesAsync(cancellationToken);

        Assert.Equal([true, false], releases.Select(release => release.Mandatory));
    }

    [Fact]
    public async Task A_second_call_within_the_cache_window_sends_no_request()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHttpHandler().Reply(ReleasesJson("v1.0.0"));
        var source = CreateSource(handler);

        await source.GetReleasesAsync(cancellationToken);
        var cached = await source.GetReleasesAsync(cancellationToken);

        Assert.Single(handler.Requests);
        Assert.Equal("1.0.0", Assert.Single(cached).Version);
    }

    [Fact]
    public async Task OpenAssetAsync_resolves_the_download_url_from_the_tag_and_streams_it()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHttpHandler()
            .Reply(ReleaseWithAssetJson())
            .Reply("binary-bytes", contentType: "application/octet-stream");
        var source = CreateSource(handler);

        await using var assetStream = await source.OpenAssetAsync("1.2.0", Binary, cancellationToken);

        Assert.NotNull(assetStream);
        using var reader = new StreamReader(assetStream);
        Assert.Equal("binary-bytes", await reader.ReadToEndAsync(cancellationToken));
        Assert.Equal(
            [$"{RepoApiUrl}/releases/tags/v1.2.0", DownloadUrl],
            handler.Requests.Select(request => request.RequestUri!.ToString()));
    }

    [Fact]
    public async Task OpenAssetAsync_gives_null_for_a_missing_asset_or_release()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHttpHandler()
            .Reply(ReleaseWithAssetJson())
            .Reply("{}", HttpStatusCode.NotFound);
        var source = CreateSource(handler);

        Assert.Null(await source.OpenAssetAsync("1.2.0", "agent-aarch64", cancellationToken));
        Assert.Null(await source.OpenAssetAsync("9.9.9", Binary, cancellationToken));
        Assert.Null(await source.OpenAssetAsync("../secrets", Binary, cancellationToken));
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Sends_the_token_header_only_when_configured()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var withToken = new RecordingHttpHandler().Reply(ReleasesJson("v1.0.0"));
        var withoutToken = new RecordingHttpHandler().Reply(ReleasesJson("v1.0.0"));

        await CreateSource(withToken, token: "secret-value").GetReleasesAsync(cancellationToken);
        await CreateSource(withoutToken).GetReleasesAsync(cancellationToken);

        var authorization = Assert.Single(withToken.Requests).Headers.Authorization;
        Assert.Equal("token", authorization?.Scheme);
        Assert.Equal("secret-value", authorization?.Parameter);
        Assert.Null(Assert.Single(withoutToken.Requests).Headers.Authorization);
    }

    [Fact]
    public async Task The_token_is_not_sent_to_a_download_host_elsewhere()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHttpHandler()
            .Reply(ReleaseWithAssetJson("https://cdn.elsewhere.test/agent-armv7"))
            .Reply("binary-bytes", contentType: "application/octet-stream");
        var source = CreateSource(handler, token: "secret-value");

        await using var assetStream = await source.OpenAssetAsync("1.2.0", Binary, cancellationToken);

        Assert.NotNull(handler.Requests[0].Headers.Authorization);
        Assert.Null(handler.Requests[1].Headers.Authorization);
    }

    [Fact]
    public async Task Draft_releases_are_neither_listed_nor_served()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var draft = new { tag_name = "v2.0.0", body = "", draft = true, assets = new[] { new { name = Binary, browser_download_url = DownloadUrl } } };
        var handler = new RecordingHttpHandler()
            .Reply(JsonSerializer.Serialize(new object[] { draft, new { tag_name = "v1.0.0", body = "", draft = false } }))
            .Reply(JsonSerializer.Serialize(draft));
        var source = CreateSource(handler);

        var releases = await source.GetReleasesAsync(cancellationToken);
        var asset = await source.OpenAssetAsync("2.0.0", Binary, cancellationToken);

        Assert.Equal("1.0.0", Assert.Single(releases).Version);
        Assert.Null(asset);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task A_server_error_on_an_asset_call_throws_instead_of_reporting_a_miss()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHttpHandler()
            .Reply("down", HttpStatusCode.BadGateway, "text/plain")
            .Reply(ReleaseWithAssetJson())
            .Reply("down", HttpStatusCode.ServiceUnavailable, "text/plain");
        var source = CreateSource(handler);

        await Assert.ThrowsAsync<HttpRequestException>(() => source.OpenAssetAsync("1.2.0", Binary, cancellationToken));
        await Assert.ThrowsAsync<HttpRequestException>(() => source.OpenAssetAsync("1.2.0", Binary, cancellationToken));
    }

    [Fact]
    public async Task A_server_error_on_the_list_call_throws()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHttpHandler().Reply("boom", HttpStatusCode.InternalServerError, "text/plain");
        var source = CreateSource(handler);

        await Assert.ThrowsAsync<HttpRequestException>(() => source.GetReleasesAsync(cancellationToken));
    }

    private static ForgeReleaseSource CreateSource(RecordingHttpHandler handler, int pageSize = 50, string? token = null) =>
        new(
            handler.CreateClient(),
            Options.Create(new OtaOptions
            {
                ForgeRepoApiUrl = RepoApiUrl,
                ForgeToken = token,
                ForgePageSize = pageSize,
                Assets = { ["armv7"] = Binary },
            }),
            new MemoryCache(new MemoryCacheOptions()));

    private static string ReleasesJson(params string[] tags) =>
        JsonSerializer.Serialize(tags.Select(tag => new { tag_name = tag, body = "", assets = Array.Empty<object>() }));

    private static string ReleaseWithAssetJson(string downloadUrl = DownloadUrl) =>
        JsonSerializer.Serialize(new
        {
            tag_name = "v1.2.0",
            body = "",
            assets = new[] { new { name = Binary, browser_download_url = downloadUrl } },
        });
}
