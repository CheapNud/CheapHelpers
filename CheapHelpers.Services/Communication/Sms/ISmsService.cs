namespace CheapHelpers.Services.Communication.Sms;

/// <summary>
/// Interface for SMS messaging services
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// Sends an SMS message to the specified phone number
    /// </summary>
    /// <param name="number">Phone number in E.164 format (e.g., +1234567890)</param>
    /// <param name="body">Message content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the SMS send operation</returns>
    Task<SmsResult> SendAsync(string number, string body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Legacy method for backward compatibility
    /// </summary>
    [Obsolete("Use SendAsync instead for better error handling and performance")]
    Task Send(string number, string body);

    /// <summary>
    /// Sends SMS messages to multiple recipients
    /// </summary>
    /// <param name="recipients">Dictionary of phone numbers and their respective messages</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of phone numbers and their send results</returns>
    Task<Dictionary<string, SmsResult>> SendBulkAsync(
        Dictionary<string, string> recipients,
        CancellationToken cancellationToken = default);
}