using CheapHelpers.Settings;

namespace CheapHelpers.Tests;

/// <summary>Pins that a value the service cannot deserialise is never replaced by a read, in either corrupt-value mode.</summary>
public sealed class FileSettingsServiceTests : IDisposable
{
    private const string CorruptJson = """{ "count": "not a number", "Section": { "number": "also not a number" } }""";

    private readonly DirectoryInfo _folder = Directory.CreateTempSubdirectory("cheaphelpers-settings-");

    private string SettingsPath => Path.Combine(_folder.FullName, "settings.json");

    public sealed class Section
    {
        public int Number { get; set; }
    }

    public void Dispose() => _folder.Delete(recursive: true);

    private FileSettingsService CreateService(CorruptValueHandling handling)
    {
        File.WriteAllText(SettingsPath, CorruptJson);
        return new FileSettingsService(new SettingsServiceOptions { Folder = _folder.FullName, OnCorruptValue = handling });
    }

    [Fact]
    public async Task Get_throws_by_default_naming_the_key_and_leaves_the_file_alone()
    {
        using var service = CreateService(new SettingsServiceOptions().OnCorruptValue);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetAsync<int>("count"));

        Assert.Contains("'count'", error.Message);
        Assert.IsAssignableFrom<System.Text.Json.JsonException>(error.InnerException);
        Assert.Equal(CorruptJson, await File.ReadAllTextAsync(SettingsPath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetSection_throws_in_throw_mode_and_leaves_the_file_alone()
    {
        using var service = CreateService(CorruptValueHandling.Throw);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetSectionAsync<Section>());

        Assert.Contains("'Section'", error.Message);
        Assert.Equal(CorruptJson, await File.ReadAllTextAsync(SettingsPath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReturnDefault_mode_returns_defaults_and_leaves_the_file_alone()
    {
        using var service = CreateService(CorruptValueHandling.ReturnDefault);

        Assert.Equal(7, await service.GetAsync("count", 7));
        Assert.Equal(0, (await service.GetSectionAsync<Section>()).Number);
        Assert.Equal(CorruptJson, await File.ReadAllTextAsync(SettingsPath, TestContext.Current.CancellationToken));
    }

    private const string BrokenFile = "{ \"count\": 1, ";

    private FileSettingsService CreateServiceOverBrokenFile(CorruptValueHandling handling)
    {
        File.WriteAllText(SettingsPath, BrokenFile);
        return new FileSettingsService(new SettingsServiceOptions { Folder = _folder.FullName, OnCorruptValue = handling });
    }

    [Fact]
    public async Task Unparseable_file_throws_in_throw_mode_naming_the_path_and_stays_in_place()
    {
        using var service = CreateServiceOverBrokenFile(CorruptValueHandling.Throw);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetAsync<int>("count"));

        Assert.Contains(SettingsPath, error.Message);
        Assert.Equal(BrokenFile, await File.ReadAllTextAsync(SettingsPath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Unparseable_file_is_moved_aside_in_return_default_mode_and_survives_a_save()
    {
        using var service = CreateServiceOverBrokenFile(CorruptValueHandling.ReturnDefault);

        Assert.Equal(5, await service.GetAsync("count", 5));
        await service.SetAsync("count", 2);

        var aside = Assert.Single(_folder.GetFiles("settings.json.corrupt-*"));
        Assert.Matches(@"^settings\.json\.corrupt-\d{14}$", aside.Name);
        Assert.Equal(BrokenFile, await File.ReadAllTextAsync(aside.FullName, TestContext.Current.CancellationToken));
        Assert.Contains("\"count\": 2", await File.ReadAllTextAsync(SettingsPath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Writing_another_key_after_a_failed_read_keeps_the_unreadable_value()
    {
        using var service = CreateService(CorruptValueHandling.ReturnDefault);

        await service.GetAsync<int>("count");
        await service.SetAsync("other", 1);

        var saved = await File.ReadAllTextAsync(SettingsPath, TestContext.Current.CancellationToken);
        Assert.Contains("\"not a number\"", saved);
        Assert.Contains("\"also not a number\"", saved);
    }
}
