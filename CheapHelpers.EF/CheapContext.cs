using CheapHelpers;
using CheapHelpers.EF.Infrastructure;
using CheapHelpers.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Hosting;

namespace CheapHelpers.EF;

/// <summary>
/// Base context with Identity and user configuration only.
/// Use this when you only need authentication without notifications, billing, or reporting.
/// <para>Inheritance chain: <c>CheapContext → CheapCommunicationContext → CheapBusinessContext</c></para>
/// </summary>
public class CheapContext<TUser> : IdentityDbContext<TUser> where TUser : IdentityUser
{
    private readonly CheapContextOptions _contextOptions;
    private bool _timeoutSet;

    public CheapContext(
        DbContextOptions<CheapContext<TUser>> options,
        CheapContextOptions? contextOptions = null)
        : base(options)
    {
        _contextOptions = contextOptions ?? new CheapContextOptions();
    }

    /// <summary>
    /// Protected constructor for derived contexts.
    /// </summary>
    protected CheapContext(DbContextOptions options, CheapContextOptions? contextOptions = null)
        : base(options)
    {
        _contextOptions = contextOptions ?? new CheapContextOptions();
    }

    public override DatabaseFacade Database
    {
        get
        {
            var db = base.Database;
            if (!_timeoutSet && IsInDev())
            {
                try
                {
                    db.SetCommandTimeout(_contextOptions.DevCommandTimeoutMs);
                    _timeoutSet = true;
                }
                catch
                {
                    // Ignore if not ready yet
                }
            }
            return db;
        }
    }

    public string? ConnectionString => base.Database?.GetConnectionString();

    protected bool IsInDev()
    {
        var environmentName = Environment.GetEnvironmentVariable(_contextOptions.EnvironmentVariable);
        return string.Equals(environmentName, Environments.Development, StringComparison.OrdinalIgnoreCase);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (IsInDev() && _contextOptions.EnableSensitiveDataLogging)
        {
            optionsBuilder.EnableSensitiveDataLogging();
        }

        optionsBuilder.ConfigureWarnings(x =>
        {
            x.Ignore(CoreEventId.RowLimitingOperationWithoutOrderByWarning);
            x.Ignore(RelationalEventId.MultipleCollectionIncludeWarning);
        });
    }

    public override int SaveChanges()
    {
        if (_contextOptions.EnableAuditing)
            AuditHelper.ApplyAuditTimestamps(ChangeTracker);
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_contextOptions.EnableAuditing)
            AuditHelper.ApplyAuditTimestamps(ChangeTracker);
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure CheapUser navigation state
        modelBuilder.Entity<TUser>(entity =>
        {
            if (typeof(TUser).GetProperty(Constants.Authentication.NavigationStateJsonColumn) != null)
            {
                entity.Property(Constants.Authentication.NavigationStateJsonColumn)
                    .HasColumnType(Constants.Database.TextColumnType)
                    .HasDefaultValue(Constants.Authentication.EmptyJsonObject);
            }
        });
    }

    /// <summary>
    /// Returns the appropriate UTC now SQL function based on the configured database provider.
    /// </summary>
    protected string GetUtcNowFunction()
    {
        var providerName = Database.ProviderName;
        return providerName switch
        {
            Constants.Database.ProviderNames.SqlServer => Constants.Database.SqlServerUtcNowFunction,
            Constants.Database.ProviderNames.Npgsql => Constants.Database.NpgsqlUtcNowFunction,
            _ => Constants.Database.SqliteUtcNowFunction
        };
    }
}
