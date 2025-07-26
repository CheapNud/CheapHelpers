using System.ComponentModel.DataAnnotations;

namespace CheapHelpers.Services.Communication.Sms;

/// <summary>
/// Configuration options for Twilio SMS service
/// </summary>
public class TwilioSmsOptions
{
    public const string SectionName = "TwilioSms";

    [Required]
    public string AccountSid { get; set; } = string.Empty;

    [Required]
    public string AuthToken { get; set; } = string.Empty;

    [Required]
    public string FromPhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of retry attempts for failed messages
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts in milliseconds
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Maximum length for SMS message body
    /// </summary>
    public int MaxMessageLength { get; set; } = 1600; // Twilio's max for SMS
}
