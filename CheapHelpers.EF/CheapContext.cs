using CheapHelpers;
using CheapHelpers.EF.Infrastructure;
using CheapHelpers.Models.Contracts;
using CheapHelpers.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Hosting;

namespace CheapHelpers.EF
{
    public class CheapContext<TUser>(
        DbContextOptions<CheapContext<TUser>> options,
        CheapContextOptions? contextOptions = null)
        : IdentityDbContext<TUser>(options) where TUser : IdentityUser
    {
        private readonly CheapContextOptions _contextOptions = contextOptions ?? new CheapContextOptions();

        private bool _timeoutSet = false;

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

        private bool IsInDev()
        {
            var environmentName = Environment.GetEnvironmentVariable(_contextOptions.EnvironmentVariable);
            return string.Equals(environmentName, Environments.Development, StringComparison.OrdinalIgnoreCase);
        }

        public string? ConnectionString => base.Database?.GetConnectionString();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (IsInDev())
            {
                if (_contextOptions.EnableSensitiveDataLogging)
                {
                    optionsBuilder.EnableSensitiveDataLogging();
                }
            }

            optionsBuilder.ConfigureWarnings(x =>
            {
                x.Ignore(CoreEventId.RowLimitingOperationWithoutOrderByWarning);
                x.Ignore(RelationalEventId.MultipleCollectionIncludeWarning);
            });

            //Paste the following line in the Package Manager Console a couple of times to change the environment to Development
            //ONLY MAKE MIGRATIONS IN THE DEVELOPMENT ENVIRONMENT
            //$Env:ASPNETCORE_ENVIRONMENT = "Development" to change connectionstring

            //Paste the following line in the Package Manager Console a couple of times to change the environment to prodcution
            //Once this is done, you can use "Update Database" to add the migrations that have been made to production.
            //$Env:ASPNETCORE_ENVIRONMENT = "Production" to change connectionstring

            //Update-Database -Args '--environment Development'

            //if the connection string is passed from somewhere else use this -> wpf, console, uwp, xamarin, NOT WEB
            //     options.UseSqlServer(_connectionString);
        }

        // Common audit fields for all contexts
        public override int SaveChanges()
        {
            if (_contextOptions.EnableAuditing)
            {
                AuditHelper.ApplyAuditTimestamps(ChangeTracker);
            }
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (_contextOptions.EnableAuditing)
            {
                AuditHelper.ApplyAuditTimestamps(ChangeTracker);
            }
            return base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<BarcodeCutSort>().HasKey(x => x.BarcodeId);
            base.OnModelCreating(modelBuilder);

            //ignores
            //modelBuilder.Entity<Customer>().Ignore(x => x.CustomerAddress);

            //foreign keys
            //modelBuilder.Entity<OldModel>().HasOne(e => e.ParentModel);

            //computes
            //modelBuilder.Entity<IdentityUser>().Property(u => u.FullName).HasComputedColumnSql("[FirstName] + ' ' + [LastName]");

            //auto includes
            //modelBuilder.Entity<ApplicationUser>().Navigation(e => e.ApplicationUserModels).AutoInclude();

            //defaults
            //modelBuilder.Entity<ApplicationUser>().Property(x => x.ReceiveMailServiceStatus).HasDefaultValue(false);
            //modelBuilder.Entity<ApplicationUser>().Property(x => x.ReceiveMailWrongSupply).HasDefaultValue(false);
            //modelBuilder.Entity<ApplicationUser>().Property(x => x.SendMailServiceRequest).HasDefaultValue(true);

            //unique constraints
            //modelBuilder.Entity<GlobalTradeItem>().HasIndex("Code", "Discriminator").IsUnique();
            //modelBuilder.Entity<Customer>().HasIndex("Code", "Discriminator").IsUnique();

            //required constraints
            //modelBuilder.Entity<ApplicationUserCustomer>().Property(x => x.ApplicationUserId).IsRequired();

            //precisions
            //modelBuilder.Entity<VarietyItemOperation>().Property(b => b.Price).HasPrecision(18, 2);

            //seed
            //modelBuilder.Entity<FirmAddress>().HasData(
            //    new FirmAddress
            //    {
            //        Id = -3,
            //        Street = "Example streer",
            //        Zip = "456789 BE",
            //        CountryId = 1
            //    });

            modelBuilder.Entity<TUser>(entity =>
            {
                // Configure the NavigationStateJson column if the user type has it
                if (typeof(TUser).GetProperty(Constants.Authentication.NavigationStateJsonColumn) != null)
                {
                    entity.Property(Constants.Authentication.NavigationStateJsonColumn)
                        .HasColumnType(Constants.Database.TextColumnType) // Works for both SQLite and SQL Server
                        .HasDefaultValue(Constants.Authentication.EmptyJsonObject);

                    // Index for performance if needed
                    // entity.HasIndex(Constants.Authentication.NavigationStateJsonColumn)
                    //     .HasDatabaseName(Constants.Database.UsersNavigationStateIndex);
                }
            });

            // Configure FileAttachment entities
            modelBuilder.Entity<FileAttachment>(entity =>
            {
                entity.HasKey(e => e.Id);

                // String length constraints
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.MimeType).HasMaxLength(100);
                entity.Property(e => e.FileExtension).HasMaxLength(10);
                entity.Property(e => e.StoragePath).HasMaxLength(500);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Tags).HasMaxLength(1000);
                entity.Property(e => e.CreatedById).HasMaxLength(450);
                entity.Property(e => e.UpdatedById).HasMaxLength(450);

                // Indexes for performance
                entity.HasIndex(e => e.CreatedAt).HasDatabaseName(Constants.Database.FileAttachmentsCreatedAtIndex);
                entity.HasIndex(e => e.Visible).HasDatabaseName(Constants.Database.FileAttachmentsVisibleIndex);
                entity.HasIndex(e => e.MimeType).HasDatabaseName(Constants.Database.FileAttachmentsMimeTypeIndex);

                // Default values
                entity.Property(e => e.Visible).HasDefaultValue(true);
                entity.Property(e => e.DisplayIndex).HasDefaultValue(0);

                var utcNowSql = GetUtcNowFunction();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql(utcNowSql);
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql(utcNowSql);
            });


            modelBuilder.Entity<GenericFileAttachment>(entity =>
            {
                entity.Property(e => e.EntityType).HasMaxLength(50);
                entity.HasIndex(e => new { e.EntityId, e.EntityType })
                      .HasDatabaseName(Constants.Database.FileAttachmentsEntityIndex);
            });

            // Configure InAppNotification entities
            modelBuilder.Entity<InAppNotification>(entity =>
            {
                entity.HasKey(e => e.Id);

                // String length constraints
                entity.Property(e => e.NotificationType).HasMaxLength(100);
                entity.Property(e => e.Title).HasMaxLength(200);
                entity.Property(e => e.ActionUrl).HasMaxLength(500);
                entity.Property(e => e.IconUrl).HasMaxLength(500);

                // Indexes for performance
                entity.HasIndex(e => e.UserId).HasDatabaseName(Constants.Database.NotificationsUserIdIndex);
                entity.HasIndex(e => new { e.UserId, e.IsRead }).HasDatabaseName(Constants.Database.NotificationsUserIdIsReadIndex);
                entity.HasIndex(e => new { e.UserId, e.NotificationType }).HasDatabaseName(Constants.Database.NotificationsUserIdTypeIndex);
                entity.HasIndex(e => e.CreatedAt).HasDatabaseName(Constants.Database.NotificationsCreatedAtIndex);
                entity.HasIndex(e => e.ExpiresAt).HasDatabaseName(Constants.Database.NotificationsExpiresAtIndex);
            });

            // Configure UserNotificationPreference entities
            modelBuilder.Entity<UserNotificationPreference>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Unique composite index on UserId + NotificationType
                entity.HasIndex(e => new { e.UserId, e.NotificationType })
                      .IsUnique()
                      .HasDatabaseName(Constants.Database.NotificationPreferencesUserIdTypeIndex);
            });

            // Configure ApiKey entities
            modelBuilder.Entity<ApiKey>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.KeyHash).IsRequired().HasMaxLength(64);
                entity.Property(e => e.KeyPrefix).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
                entity.Property(e => e.ScopesJson).HasMaxLength(2000);

                entity.HasIndex(e => e.KeyHash)
                      .IsUnique()
                      .HasDatabaseName(Constants.Database.ApiKeysKeyHashIndex);

                entity.HasIndex(e => e.KeyPrefix)
                      .HasDatabaseName(Constants.Database.ApiKeysKeyPrefixIndex);

                entity.HasIndex(e => e.UserId)
                      .HasDatabaseName(Constants.Database.ApiKeysUserIdIndex);

                entity.HasIndex(e => new { e.UserId, e.IsActive })
                      .HasDatabaseName(Constants.Database.ApiKeysUserIdIsActiveIndex);

                entity.Property(e => e.IsActive).HasDefaultValue(true);

                var utcNowSql = GetUtcNowFunction();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql(utcNowSql);
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql(utcNowSql);
            });

            // Configure UsageRecord entities (high-volume write path, lean)
            modelBuilder.Entity<UsageRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Endpoint).HasMaxLength(500);
                entity.Property(e => e.HttpMethod).HasMaxLength(10);
                entity.HasIndex(e => new { e.ApiKeyId, e.Timestamp })
                      .HasDatabaseName("IX_UsageRecords_ApiKeyId_Timestamp");
                entity.HasIndex(e => e.Timestamp)
                      .HasDatabaseName("IX_UsageRecords_Timestamp");
            });

            // Configure UsageAggregate entities
            modelBuilder.Entity<UsageAggregate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ApiKeyId, e.PeriodStart })
                      .IsUnique()
                      .HasDatabaseName("IX_UsageAggregates_ApiKeyId_PeriodStart");

                var utcNow2 = GetUtcNowFunction();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql(utcNow2);
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql(utcNow2);
            });

            // Configure BillingPlan entities
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

                var utcNow3 = GetUtcNowFunction();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql(utcNow3);
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql(utcNow3);
            });

            // Configure BillingInvoice entities
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

                entity.HasIndex(e => e.InvoiceNumber)
                      .IsUnique()
                      .HasDatabaseName("IX_BillingInvoices_InvoiceNumber");
                entity.HasIndex(e => e.ApiKeyId)
                      .HasDatabaseName("IX_BillingInvoices_ApiKeyId");
                entity.HasIndex(e => e.Status)
                      .HasDatabaseName("IX_BillingInvoices_Status");

                var utcNow4 = GetUtcNowFunction();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql(utcNow4);
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql(utcNow4);
            });

            // Configure Report entities
            modelBuilder.Entity<Report>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.StoragePath).HasMaxLength(500);
                entity.Property(e => e.MimeType).HasMaxLength(100);
                entity.Property(e => e.GeneratedById).HasMaxLength(450);
                entity.Property(e => e.ErrorMessage).HasMaxLength(2000);

                entity.HasIndex(e => e.GeneratedById)
                      .HasDatabaseName("IX_Reports_GeneratedById");
                entity.HasIndex(e => e.ExpiresAt)
                      .HasDatabaseName("IX_Reports_ExpiresAt");

                var utcNow5 = GetUtcNowFunction();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql(utcNow5);
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql(utcNow5);
            });

        }

        /// <summary>
        /// Returns the appropriate UTC now SQL function based on the configured database provider.
        /// </summary>
        private string GetUtcNowFunction()
        {
            var providerName = Database.ProviderName;

            return providerName switch
            {
                Constants.Database.ProviderNames.SqlServer => Constants.Database.SqlServerUtcNowFunction,
                Constants.Database.ProviderNames.Npgsql => Constants.Database.NpgsqlUtcNowFunction,
                _ => Constants.Database.SqliteUtcNowFunction // SQLite as default fallback
            };
        }

        /// <summary>
        /// Gets or sets the DbSet for in-app notifications.
        /// </summary>
        public DbSet<InAppNotification> InAppNotifications { get; set; }

        /// <summary>
        /// Gets or sets the DbSet for user notification preferences.
        /// </summary>
        public DbSet<UserNotificationPreference> UserNotificationPreferences { get; set; }

        public DbSet<FileAttachment> FileAttachments { get; set; }

        public DbSet<ApiKey> ApiKeys { get; set; }

        public DbSet<UsageRecord> UsageRecords { get; set; }

        public DbSet<UsageAggregate> UsageAggregates { get; set; }

        public DbSet<BillingPlan> BillingPlans { get; set; }

        public DbSet<BillingInvoice> BillingInvoices { get; set; }

        public DbSet<Report> Reports { get; set; }
    }

    // Usage Examples:
    // 
    // Context only (no Identity):
    // services.AddCheapContext<MyUser>(options => options.UseSqlServer(connectionString));
    // 
    // Context + Identity with defaults (most common):
    // services.AddCheapContext<ApplicationUser>(options => options.UseSqlServer(connectionString))
    //     .AddIdentity<IdentityRole>();
    // 
    // Context + Identity with custom configuration:
    // services.AddCheapContext<ApplicationUser>(options => options.UseSqlServer(connectionString))
    //     .AddIdentity<IdentityRole>(options => 
    //     {
    //         options.Password.RequiredLength = 12;
    //         options.Lockout.MaxFailedAccessAttempts = 3;
    //     });
    // 
    // Full fluent chain (like Microsoft's pattern):
    // services.AddCheapContext<ApplicationUser>(options => options.UseSqlServer(connectionString))
    //     .AddIdentity<IdentityRole>(options => 
    //     {
    //         options.Password.RequiredLength = 12;
    //     })
    //     .AddDefaultUI()
    //     .AddDefaultTokenProviders()
    //     .Services  // Access underlying IServiceCollection
    //     .AddScoped<IMyService, MyService>();
    // 
    // Simple case with IdentityUser/IdentityRole:
    // services.AddCheapContext(options => options.UseSqlServer(connectionString))
    //     .AddIdentity();
    // 
    // With custom CheapContextOptions:
    // var contextOptions = new CheapContextOptions 
    // {
    //     DevCommandTimeoutMs = 300000,
    //     Identity = new IdentityOptions 
    //     {
    //         Password = new PasswordOptions { RequiredLength = 12 }
    //     }
    // };
    // services.AddCheapContext<ApplicationUser>(options => options.UseSqlServer(connectionString), contextOptions)
    //     .AddIdentity<IdentityRole>();
}