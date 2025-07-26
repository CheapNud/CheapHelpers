namespace CheapHelpers.Services.Communication.Sms;

/// <summary>
/// Result of an SMS send operation
/// </summary>
public record SmsResult
{
    public bool IsSuccess { get; init; }
    public string? MessageSid { get; init; }
    public string? ErrorMessage { get; init; }
    public Exception? Exception { get; init; }
    public string Status { get; init; } = string.Empty;

    public static SmsResult Success(string messageSid, string status)
        => new() { IsSuccess = true, MessageSid = messageSid, Status = status };

    public static SmsResult Failure(string errorMessage, Exception? exception = null)
        => new() { IsSuccess = false, ErrorMessage = errorMessage, Exception = exception };
}
