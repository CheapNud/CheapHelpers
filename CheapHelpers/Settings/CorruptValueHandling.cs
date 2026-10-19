namespace CheapHelpers.Settings;

/// <summary>
/// What <see cref="FileSettingsService"/> does when a stored value cannot be deserialised into the requested type.
/// </summary>
public enum CorruptValueHandling
{
    /// <summary>Throw an <see cref="InvalidOperationException"/> that names the key and wraps the JSON error.</summary>
    Throw,

    /// <summary>Log a warning and return the default value.</summary>
    ReturnDefault
}
