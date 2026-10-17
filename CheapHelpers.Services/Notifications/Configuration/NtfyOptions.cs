namespace CheapHelpers.Services.Notifications.Configuration;

/// <summary>
/// Configuration for the ntfy notification channel (section "Notifications:Ntfy")
/// </summary>
public class NtfyOptions
{
    /// <summary>
    /// Full topic URL, for example https://ntfy.example.com/alerts. Empty disables the channel.
    /// </summary>
    public string? TopicUrl { get; set; }

    /// <summary>
    /// Optional bearer token for protected topics.
    /// </summary>
    public string? Token { get; set; }
}
