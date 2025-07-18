using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Exceptions;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace CheapHelpers.Services.Communication.Sms;

/// <summary>
/// Enhanced Twilio SMS service with configuration, validation, and retry logic
/// </summary>
public class TwilioSmsService : ISmsService, IDisposable
{
    private readonly TwilioSmsOptions _options;
    private readonly ILogger<TwilioSmsService>? _logger;
    private readonly SemaphoreSlim _rateLimitSemaphore;
    private bool _isInitialized;
    private bool _disposed;

    // Phone number validation regex (basic E.164 format)
    private static readonly Regex PhoneNumberRegex = new(
        @"^\+[1-9]\d{1,14}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public TwilioSmsService(IOptions<TwilioSmsOptions> options, ILogger<TwilioSmsService>? logger = null)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
        _rateLimitSemaphore = new SemaphoreSlim(10, 10); // Allow up to 10 concurrent requests

        ValidateConfiguration();
        InitializeTwilioClient();
    }

    /// <summary>
    /// Sends an SMS message to the specified phone number
    /// </summary>
    /// <param name="number">Phone number in E.164 format (e.g., +1234567890)</param>
    /// <param name="body">Message content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the SMS send operation</returns>
    public async Task<SmsResult> SendAsync(
        string number,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(TwilioSmsService));

        var validationResult = ValidateInput(number, body);
        if (!validationResult.IsSuccess)
            return validationResult;

        await _rateLimitSemaphore.WaitAsync(cancellationToken);

        try
        {
            return await SendWithRetryAsync(number, body, cancellationToken);
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    /// <summary>
    /// Legacy method for backward compatibility
    /// </summary>
    [Obsolete("Use SendAsync instead for better error handling and performance")]
    public async Task Send(string number, string body)
    {
        var result = await SendAsync(number, body);
        if (!result.IsSuccess)
        {
            var exception = result.Exception ?? new InvalidOperationException(result.ErrorMessage);
            throw exception;
        }
    }

    /// <summary>
    /// Sends SMS messages to multiple recipients
    /// </summary>
    /// <param name="recipients">Dictionary of phone numbers and their respective messages</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of phone numbers and their send results</returns>
    public async Task<Dictionary<string, SmsResult>> SendBulkAsync(
        Dictionary<string, string> recipients,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(TwilioSmsService));

        if (recipients == null || recipients.Count == 0)
            throw new ArgumentException("Recipients cannot be null or empty", nameof(recipients));

        var results = new Dictionary<string, SmsResult>();
        var tasks = new List<Task>();

        foreach (var kvp in recipients)
        {
            var phoneNumber = kvp.Key;
            var message = kvp.Value;

            var task = Task.Run(async () =>
            {
                var result = await SendAsync(phoneNumber, message, cancellationToken);
                lock (results)
                {
                    results[phoneNumber] = result;
                }
            });

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
        return results;
    }

    /// <summary>
    /// Validates phone number format and message content
    /// </summary>
    private SmsResult ValidateInput(string number, string body)
    {
        if (string.IsNullOrWhiteSpace(number))
            return SmsResult.Failure("Phone number cannot be null or empty");

        if (string.IsNullOrWhiteSpace(body))
            return SmsResult.Failure("Message body cannot be null or empty");

        if (!PhoneNumberRegex.IsMatch(number))
            return SmsResult.Failure($"Invalid phone number format: {number}. Must be in E.164 format (e.g., +1234567890)");

        if (body.Length > _options.MaxMessageLength)
            return SmsResult.Failure($"Message body too long: {body.Length} characters. Maximum allowed: {_options.MaxMessageLength}");

        return SmsResult.Success(string.Empty, string.Empty);
    }

    /// <summary>
    /// Sends SMS with retry logic
    /// </summary>
    private async Task<SmsResult> SendWithRetryAsync(
        string number,
        string body,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        for (int attempt = 1; attempt <= _options.MaxRetryAttempts; attempt++)
        {
            try
            {
                _logger?.LogDebug("Sending SMS to {PhoneNumber}, attempt {Attempt}/{MaxAttempts}",
                    number, attempt, _options.MaxRetryAttempts);

                var message = await MessageResource.CreateAsync(
                    body: body,
                    from: new PhoneNumber(_options.FromPhoneNumber),
                    to: new PhoneNumber(number)
                );

                var result = SmsResult.Success(message.Sid, message.Status.ToString());

                _logger?.LogInformation("SMS sent successfully to {PhoneNumber}. MessageSid: {MessageSid}, Status: {Status}",
                    number, message.Sid, message.Status);

                Debug.WriteLine($"SMS sent successfully to {number}. MessageSid: {message.Sid}, Status: {message.Status}");

                return result;
            }
            catch (TwilioException ex)
            {
                lastException = ex;
                _logger?.LogWarning("Twilio error on attempt {Attempt}: {ErrorMessage}", attempt, ex.Message);
                Debug.WriteLine($"Twilio error on attempt {attempt}: {ex.Message}");

                // Don't retry on certain error types
                if (IsNonRetryableError(ex))
                {
                    var errorResult = SmsResult.Failure($"Non-retryable Twilio error: {ex.Message}", ex);
                    _logger?.LogError(ex, "Non-retryable Twilio error for {PhoneNumber}: {ErrorMessage}", number, ex.Message);
                    return errorResult;
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger?.LogWarning("Unexpected error on attempt {Attempt}: {ErrorMessage}", attempt, ex.Message);
                Debug.WriteLine($"Unexpected error on attempt {attempt}: {ex.Message}");
            }

            // Wait before retry (except on last attempt)
            if (attempt < _options.MaxRetryAttempts)
            {
                await Task.Delay(_options.RetryDelayMs * attempt, cancellationToken);
            }
        }

        var finalResult = SmsResult.Failure($"Failed to send SMS after {_options.MaxRetryAttempts} attempts. Last error: {lastException?.Message}", lastException);
        _logger?.LogError(lastException, "Failed to send SMS to {PhoneNumber} after {MaxAttempts} attempts", number, _options.MaxRetryAttempts);

        return finalResult;
    }

    /// <summary>
    /// Determines if a Twilio exception should not be retried
    /// </summary>
    private static bool IsNonRetryableError(TwilioException ex)
    {
        // Check error message patterns for non-retryable errors
        var nonRetryablePatterns = new[]
        {
            "invalid phone number",
            "unsubscribed recipient",
            "blocked",
            "carrier violation",
            "invalid parameters"
        };

        var errorMessage = ex.Message?.ToLowerInvariant() ?? string.Empty;

        foreach (var pattern in nonRetryablePatterns)
        {
            if (errorMessage.Contains(pattern))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Validates the Twilio configuration
    /// </summary>
    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.AccountSid))
            throw new InvalidOperationException("Twilio AccountSid is required");

        if (string.IsNullOrWhiteSpace(_options.AuthToken))
            throw new InvalidOperationException("Twilio AuthToken is required");

        if (string.IsNullOrWhiteSpace(_options.FromPhoneNumber))
            throw new InvalidOperationException("Twilio FromPhoneNumber is required");

        if (!PhoneNumberRegex.IsMatch(_options.FromPhoneNumber))
            throw new InvalidOperationException($"Invalid FromPhoneNumber format: {_options.FromPhoneNumber}");

        if (_options.MaxRetryAttempts < 1)
            throw new InvalidOperationException("MaxRetryAttempts must be at least 1");

        if (_options.RetryDelayMs < 0)
            throw new InvalidOperationException("RetryDelayMs cannot be negative");
    }

    /// <summary>
    /// Initializes the Twilio client
    /// </summary>
    private void InitializeTwilioClient()
    {
        if (_isInitialized) return;

        try
        {
            TwilioClient.Init(_options.AccountSid, _options.AuthToken);
            _isInitialized = true;

            _logger?.LogInformation("Twilio client initialized successfully");
            Debug.WriteLine("Twilio client initialized successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize Twilio client");
            Debug.WriteLine($"Failed to initialize Twilio client: {ex.Message}");
            throw new InvalidOperationException("Failed to initialize Twilio client", ex);
        }
    }

    /// <summary>
    /// Disposes resources
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _rateLimitSemaphore?.Dispose();
        _disposed = true;
    }
}