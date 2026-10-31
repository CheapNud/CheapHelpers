namespace CheapHelpers.Models.Dtos.Ubl;

/// <summary>
/// Payment means specification. Common codes: "30" = bank transfer, "58" = SEPA credit transfer.
/// </summary>
public record UblPaymentMeans
{
    public string PaymentMeansCode { get; init; } = "30";

    /// <summary>
    /// Structured payment reference (e.g., Belgian +++OGM+++ format).
    /// </summary>
    public string? PaymentId { get; init; }

    public UblFinancialAccount? PayeeFinancialAccount { get; init; }
}
