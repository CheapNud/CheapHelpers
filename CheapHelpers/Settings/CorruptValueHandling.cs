namespace CheapHelpers.Settings;

/// <summary>
/// What <see cref="FileSettingsService"/> does when a stored value cannot be deserialised into the requested type,
/// or when the settings file itself is not a JSON object.
/// </summary>
public enum CorruptValueHandling
{
    /// <summary>Throw an <see cref="InvalidOperationException"/> that names the key (or the file) and wraps the JSON error.</summary>
    Throw,

    /// <summary>Log a warning and return the default value; a broken file is first moved aside so it is never overwritten.</summary>
    ReturnDefault
}
