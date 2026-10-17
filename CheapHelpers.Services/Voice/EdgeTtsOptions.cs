namespace CheapHelpers.Services.Voice;

/// <summary>
/// Configuration for <see cref="EdgeTextToSpeech"/>. Rate, pitch and volume are SSML prosody values.
/// </summary>
public class EdgeTtsOptions
{
    public const string SectionName = "Voice:Edge";

    public string Voice { get; set; } = "en-US-AriaNeural";

    /// <summary>Relative speaking rate, e.g. "+10%" or "-5%".</summary>
    public string Rate { get; set; } = "+0%";

    /// <summary>Relative pitch, e.g. "+5Hz".</summary>
    public string Pitch { get; set; } = "+0Hz";

    /// <summary>Relative volume, e.g. "-20%".</summary>
    public string Volume { get; set; } = "+0%";

    /// <summary>Time limit for the whole synthesis, including the one clock-skew retry. On expiry the call returns empty audio.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
