namespace CheapHelpers.Services.DataExchange.Ubl.Validation;

/// <summary>
/// Validation result for PEPPOL BIS 3.0 invoice/credit note validation
/// </summary>
public record PeppolValidationResult
{
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = [];

    public static PeppolValidationResult Success() => new() { IsValid = true };

    public static PeppolValidationResult Failed(List<string> errors) => new()
    {
        IsValid = false,
        Errors = errors
    };
}
