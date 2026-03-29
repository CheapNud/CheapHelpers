using CheapHelpers;
using CheapHelpers.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CheapHelpers.EF.Infrastructure;

namespace CheapHelpers.EF;

/// <summary>
/// Full context with Identity + communications + API keys, billing, and reporting.
/// Use this for enterprise apps that need the complete CheapHelpers feature set.
/// <para>Inheritance chain: <c>CheapContext → CheapCommunicationContext → CheapBusinessContext</c></para>
/// </summary>
public class CheapBusinessContext<TUser> : CheapCommunicationContext<TUser> where TUser : IdentityUser
{
    public CheapBusinessContext(
        DbContextOptions<CheapBusinessContext<TUser>> options,
        CheapContextOptions? contextOptions = null)
        : base(options, contextOptions) { }

    protected CheapBusinessContext(DbContextOptions options, CheapContextOptions? contextOptions = null)
        : base(options, contextOptions) { }

    public DbSet<ApiKey> ApiKeys { get; set; }
    public DbSet<UsageRecord> UsageRecords { get; set; }
    public DbSet<UsageAggregate> UsageAggregates { get; set; }
    public DbSet<BillingPlan> BillingPlans { get; set; }
    public DbSet<BillingInvoice> BillingInvoices { get; set; }
    public DbSet<Report> Reports { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var utcNow = GetUtcNowFunction();

        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.KeyHash).IsRequired().HasMaxLength(64);
            entity.Property(e => e.KeyPrefix).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.ScopesJson).HasMaxLength(2000);
            entity.HasIndex(e => e.KeyHash).IsUnique().HasDatabaseName(Constants.Database.ApiKeysKeyHashIndex);
            entity.HasIndex(e => e.KeyPrefix).HasDatabaseName(Constants.Database.ApiKeysKeyPrefixIndex);
            entity.HasIndex(e => e.UserId).HasDatabaseName(Constants.Database.ApiKeysUserIdIndex);
            entity.HasIndex(e => new { e.UserId, e.IsActive }).HasDatabaseName(Constants.Database.ApiKeysUserIdIsActiveIndex);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(utcNow);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql(utcNow);
        });

        modelBuilder.Entity<UsageRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Endpoint).HasMaxLength(500);
            entity.Property(e => e.HttpMethod).HasMaxLength(10);
            entity.HasIndex(e => new { e.ApiKeyId, e.Timestamp }).HasDatabaseName("IX_UsageRecords_ApiKeyId_Timestamp");
            entity.HasIndex(e => e.Timestamp).HasDatabaseName("IX_UsageRecords_Timestamp");
        });

        modelBuilder.Entity<UsageAggregate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ApiKeyId, e.PeriodStart }).IsUnique().HasDatabaseName("IX_UsageAggregates_ApiKeyId_PeriodStart");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(utcNow);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql(utcNow);
        });

        modelBuilder.Entity<BillingPlan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Currency).HasMaxLength(3);
            entity.Property(e => e.RatePerUnit).HasPrecision(18, 6);
            entity.Property(e => e.OverageRate).HasPrecision(18, 6);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasIndex(e => e.Name).HasDatabaseName("IX_BillingPlans_Name");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(utcNow);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql(utcNow);
        });

        modelBuilder.Entity<BillingInvoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InvoiceNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Currency).HasMaxLength(3);
            entity.Property(e => e.SubTotal).HasPrecision(18, 2);
            entity.Property(e => e.TaxAmount).HasPrecision(18, 2);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.UblXml).HasColumnType(Constants.Database.TextColumnType);
            entity.Property(e => e.PdfStoragePath).HasMaxLength(500);
            entity.HasIndex(e => e.InvoiceNumber).IsUnique().HasDatabaseName("IX_BillingInvoices_InvoiceNumber");
            entity.HasIndex(e => e.ApiKeyId).HasDatabaseName("IX_BillingInvoices_ApiKeyId");
            entity.HasIndex(e => e.Status).HasDatabaseName("IX_BillingInvoices_Status");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(utcNow);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql(utcNow);
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.StoragePath).HasMaxLength(500);
            entity.Property(e => e.MimeType).HasMaxLength(100);
            entity.Property(e => e.GeneratedById).HasMaxLength(450);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.HasIndex(e => e.GeneratedById).HasDatabaseName("IX_Reports_GeneratedById");
            entity.HasIndex(e => e.ExpiresAt).HasDatabaseName("IX_Reports_ExpiresAt");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(utcNow);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql(utcNow);
        });
    }
}
