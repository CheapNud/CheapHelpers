using CheapHelpers.Ota;
using Microsoft.Extensions.Options;

namespace CheapHelpers.Tests.Ota;

public sealed class LocalFolderReleaseSourceTests : IDisposable
{
    private const string Binary = "agent-armv7";

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"ota-{Guid.NewGuid():N}");
    private readonly string _outside;
    private readonly LocalFolderReleaseSource _source;

    public LocalFolderReleaseSourceTests()
    {
        Directory.CreateDirectory(_root);
        _outside = _root + "-outside";
        Directory.CreateDirectory(_outside);
        File.WriteAllText(Path.Combine(_outside, Binary), "outside");

        _source = new LocalFolderReleaseSource(Options.Create(new OtaOptions
        {
            Source = OtaSourceKind.LocalFolder,
            LocalPath = _root,
            Assets = { ["armv7"] = Binary },
        }));
    }

    [Fact]
    public async Task Lists_parsable_version_folders_newest_first_with_the_mandatory_marker()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(Path.Combine(_root, "1.2.0"));
        Directory.CreateDirectory(Path.Combine(_root, "1.10.0"));
        Directory.CreateDirectory(Path.Combine(_root, "1.3.0"));
        Directory.CreateDirectory(Path.Combine(_root, "notes"));
        File.WriteAllText(Path.Combine(_root, "1.3.0", "mandatory"), "");

        var releases = await _source.GetReleasesAsync(cancellationToken);

        Assert.Equal(
            [new OtaRelease("1.10.0", "v1.10.0", false), new OtaRelease("1.3.0", "v1.3.0", true), new OtaRelease("1.2.0", "v1.2.0", false)],
            releases);
    }

    [Fact]
    public async Task Opens_an_existing_asset_and_gives_null_for_a_missing_one()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(Path.Combine(_root, "1.0.0"));
        File.WriteAllText(Path.Combine(_root, "1.0.0", Binary), "binary");

        await using (var assetStream = await _source.OpenAssetAsync("1.0.0", Binary, cancellationToken))
        {
            Assert.NotNull(assetStream);
            using var reader = new StreamReader(assetStream);
            Assert.Equal("binary", await reader.ReadToEndAsync(cancellationToken));
        }

        Assert.Null(await _source.OpenAssetAsync("1.0.0", Binary + ".sha256", cancellationToken));
        Assert.Null(await _source.OpenAssetAsync("2.0.0", Binary, cancellationToken));
    }

    [Theory]
    [InlineData("1.0.0", "../../" + Binary)]
    [InlineData("1.0.0", "install.sh")]
    [InlineData("..", Binary)]
    public async Task Unknown_names_and_escapes_give_null(string version, string assetName)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(Path.Combine(_root, "1.0.0"));
        File.WriteAllText(Path.Combine(_root, "1.0.0", "install.sh"), "script");

        Assert.Null(await _source.OpenAssetAsync(version, assetName, cancellationToken));
    }

    [Fact]
    public async Task A_configured_asset_that_points_outside_the_folder_is_never_opened()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(Path.Combine(_root, "1.0.0"));
        var escapingAsset = $"../../{Path.GetFileName(_outside)}/{Binary}";
        var escapingSource = new LocalFolderReleaseSource(Options.Create(new OtaOptions
        {
            Source = OtaSourceKind.LocalFolder,
            LocalPath = _root,
            Assets = { ["armv7"] = escapingAsset },
        }));

        Assert.True(File.Exists(Path.Combine(_root, "1.0.0", escapingAsset)));
        Assert.Null(await escapingSource.OpenAssetAsync("1.0.0", escapingAsset, cancellationToken));
    }

    [Fact]
    public async Task A_missing_folder_gives_an_empty_list()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        Directory.Delete(_root);

        Assert.Empty(await _source.GetReleasesAsync(cancellationToken));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
        Directory.Delete(_outside, recursive: true);
    }
}
