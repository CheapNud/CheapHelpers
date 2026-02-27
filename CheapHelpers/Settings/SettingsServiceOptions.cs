namespace CheapHelpers.Settings;

/// <summary>
/// Configuration options for <see cref="FileSettingsService"/>.
/// </summary>
public class SettingsServiceOptions
{
    /// <summary>
    /// Application name used as the settings folder name under AppData.
    /// Falls back to "App" if null or whitespace.
    /// </summary>
    public string? AppName { get; set; }

    /// <summary>
    /// Full path override for the settings folder. When set, <see cref="AppName"/> is ignored.
    /// </summary>
    public string? Folder { get; set; }

    /// <summary>
    /// File name for the settings JSON file (default: "settings.json")
    /// </summary>
    public string FileName { get; set; } = "settings.json";

    /// <summary>
    /// Automatically save settings after every Set/Delete/SetSection/UpdateSection call (default: true)
    /// </summary>
    public bool AutoSave { get; set; } = true;
}
