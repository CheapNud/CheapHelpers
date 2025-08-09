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
                AddAuditInfo();
            }
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (_contextOptions.EnableAuditing)
            {
                AddAuditInfo();
            }
            return base.SaveChangesAsync(cancellationToken);
        }

        private void AddAuditInfo()
        {
            var auditableEntries = ChangeTracker.Entries()
                .Where(e => e.Entity is IAuditable &&
                           e.State is EntityState.Added or EntityState.Modified);

            if (!auditableEntries.Any()) return;

            var now = DateTime.UtcNow;

            foreach (var entry in auditableEntries)
            {
                var auditable = (IAuditable)entry.Entity;

                if (entry.State == EntityState.Added)
                {
                    auditable.CreatedAt = now;
                }

                auditable.UpdatedAt = now;
            }
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
                if (typeof(TUser).GetProperty("NavigationStateJson") != null)
                {
                    entity.Property("NavigationStateJson")
                        .HasColumnType("TEXT") // Works for both SQLite and SQL Server
                        .HasDefaultValue("{}");

                    // Index for performance if needed
                    // entity.HasIndex("NavigationStateJson")
                    //     .HasDatabaseName("IX_Users_NavigationState");
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
                entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_FileAttachments_CreatedAt");
                entity.HasIndex(e => e.Visible).HasDatabaseName("IX_FileAttachments_Visible");
                entity.HasIndex(e => e.MimeType).HasDatabaseName("IX_FileAttachments_MimeType");

                // Default values
                entity.Property(e => e.Visible).HasDefaultValue(true);
                entity.Property(e => e.DisplayIndex).HasDefaultValue(0);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("DATETIME('now')"); // SQLite
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("DATETIME('now')"); // SQLite

                // For SQL Server, use these instead:
                // entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                // entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            });


            modelBuilder.Entity<GenericFileAttachment>(entity =>
            {
                entity.Property(e => e.EntityType).HasMaxLength(50);
                entity.HasIndex(e => new { e.EntityId, e.EntityType })
                      .HasDatabaseName("IX_FileAttachments_Entity");
            });

        }

        public DbSet<FileAttachment> FileAttachments { get; set; }
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