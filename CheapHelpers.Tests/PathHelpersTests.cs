using CheapHelpers.Helpers;

namespace CheapHelpers.Tests;

/// <summary>Pins containment, unique naming and the wait-until-ready check against a real temp folder.</summary>
public sealed class PathHelpersTests : IDisposable
{
    private readonly DirectoryInfo _folder = Directory.CreateTempSubdirectory("cheaphelpers-paths-");

    public void Dispose() => _folder.Delete(recursive: true);

    private string Root => Path.Combine(_folder.FullName, "data");

    [Fact]
    public void Root_and_paths_below_it_are_contained()
    {
        Assert.True(PathHelpers.IsContainedIn(Root, Root));
        Assert.True(PathHelpers.IsContainedIn(Path.Combine(Root, "a", "b.txt"), Root));
        Assert.True(PathHelpers.IsContainedIn(Path.Combine(Root, "..data-file"), Root));
    }

    [Fact]
    public void Traversal_and_siblings_are_rejected()
    {
        Assert.False(PathHelpers.IsContainedIn(Path.Combine(Root, "..", "secrets.txt"), Root));
        Assert.False(PathHelpers.IsContainedIn(Path.Combine(Root, "a", "..", "..", "other"), Root));
        Assert.False(PathHelpers.IsContainedIn(Root + "-backup", Root));
        Assert.False(PathHelpers.IsContainedIn(_folder.FullName, Root));
    }

    [Fact]
    public void Unique_path_appends_an_increasing_counter()
    {
        var original = Path.Combine(_folder.FullName, "report.pdf");
        Assert.Equal(original, PathHelpers.GetUniqueFilePath(original));

        File.WriteAllText(original, "");
        var second = PathHelpers.GetUniqueFilePath(original);
        Assert.Equal(Path.Combine(_folder.FullName, "report (2).pdf"), second);

        File.WriteAllText(second, "");
        Assert.Equal(Path.Combine(_folder.FullName, "report (3).pdf"), PathHelpers.GetUniqueFilePath(original));
    }

    [Fact]
    public async Task Wait_until_ready_succeeds_for_a_free_file()
    {
        var path = Path.Combine(_folder.FullName, "free.txt");
        await File.WriteAllTextAsync(path, "done", TestContext.Current.CancellationToken);

        Assert.True(await PathHelpers.WaitUntilReadyAsync(path, TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Wait_until_ready_times_out_for_a_missing_file()
    {
        var path = Path.Combine(_folder.FullName, "missing.txt");

        Assert.False(await PathHelpers.WaitUntilReadyAsync(path, TimeSpan.FromMilliseconds(300), TestContext.Current.CancellationToken));
    }
}
