namespace CheapHelpers.Services.Billing.Configuration;

/// <summary>
/// Configuration options for the CheapHelpers billing system.
/// </summary>
public class BillingOptions
{
    /// <summary>
    /// The configuration section name used in appsettings.json.
    /// </summary>
    public const string SectionName = "Billing";

    /// <summary>
    /// Day of the month on which billing cycles run (1-28 recommended).
    /// </summary>
    public int BillingDayOfMonth { get; set; } = 1;

    /// <summary>
    /// Time of day (UTC) at which billing runs execute.
    /// </summary>
    public TimeOnly BillingRunTime { get; set; } = new(2, 0);

    /// <summary>
    /// Prefix prepended to sequential invoice numbers (e.g. "INV" produces "INV-2026-000001").
    /// </summary>
    public string InvoiceNumberPrefix { get; set; } = "INV";

    /// <summary>
    /// Default tax rate applied to invoices, as a percentage. Belgian VAT default.
    /// </summary>
    public decimal DefaultTaxRate { get; set; } = 21.00m;

    /// <summary>
    /// ISO 4217 currency code used for invoice amounts.
    /// </summary>
    public string DefaultCurrency { get; set; } = "EUR";

    /// <summary>
    /// Number of days to retain raw usage records before cleanup.
    /// </summary>
    public int UsageRetentionDays { get; set; } = 90;
}
