using CheapHelpers.EF.Infrastructure;
using CheapHelpers.Models.Contracts;
using CheapHelpers.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace CheapHelpers.EF
{


    namespace CheapHelpers.EF
    {
        public class CheapContext<TUser>(
            DbContextOptions<CheapContext<TUser>> options,
            CheapContextOptions? contextOptions = null)
            : IdentityDbContext<TUser>(options) where TUser : IdentityUser
        {
            private readonly CheapContextOptions _contextOptions = contextOptions ?? new CheapContextOptions();

            private bool IsInDev()
            {
                var environmentName = Environment.GetEnvironmentVariable(_contextOptions.EnvironmentVariable);
                return string.Equals(environmentName, Environments.Development, StringComparison.OrdinalIgnoreCase);
            }

            public string? ConnectionString => Database?.GetConnectionString();

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                if (IsInDev())
                {
                    Database.SetCommandTimeout(_contextOptions.DevCommandTimeoutMs);

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
            }

            public DbSet<CheapUser> Users { get; set; }
            public DbSet<FileAttachment> FileAttachments { get; set; }
        }

        // Usage Examples:
        // 
        // Basic usage with defaults:
        // services.AddDbContext<CheapContext<IdentityUser>>(options => options.UseSqlServer(connectionString));
        // 
        // With custom options:
        // var contextOptions = new CheapContextOptions 
        // {
        //     DevCommandTimeoutMs = 300000,
        //     EnableAuditing = false,
        //     EnvironmentVariable = "MY_CUSTOM_ENV"
        // };
        // services.AddSingleton(contextOptions);
        // services.AddDbContext<CheapContext<IdentityUser>>(options => options.UseSqlServer(connectionString));
        //
        // Or direct instantiation:
        // var context = new CheapContext<MyUser>(options, contextOptions);
    }