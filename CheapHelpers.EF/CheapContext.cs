using CheapHelpers.Models.Contracts;
using CheapHelpers.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CheapHelpers.EF
{
    public class CheapContextOptions
    {
        public string EnvironmentVariable { get; set; } = "ASPNETCORE_ENVIRONMENT";
        public int DevCommandTimeoutMs { get; set; } = 150000;
        public bool EnableAuditing { get; set; } = true;
        public bool EnableSensitiveDataLogging { get; set; } = true;

        // Use the real IdentityOptions with your defaults
        public IdentityOptions Identity { get; set; } = new()
        {
            Password = new PasswordOptions
            {
                RequireDigit = true,
                RequireLowercase = true,
                RequireNonAlphanumeric = false,
                RequireUppercase = true,
                RequiredLength = 8,
                RequiredUniqueChars = 1
            },
            SignIn = new SignInOptions
            {
                RequireConfirmedAccount = false
            },
            Lockout = new LockoutOptions
            {
                DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5),
                MaxFailedAccessAttempts = 8,
                AllowedForNewUsers = true
            },
            User = new UserOptions
            {
                AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+",
                RequireUniqueEmail = true
            }
        };
    }
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

        public string ConnectionString => Database.GetConnectionString();

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

    // Extension methods for easy setup
    public static class CheapContextServiceExtensions
    {
        /// <summary>
        /// Adds CheapContext with the specified user type. Chain .AddIdentity() for Identity services.
        /// </summary>
        public static CheapContextBuilder<TUser> AddCheapContext<TUser>(
            this IServiceCollection services,
            Action<DbContextOptionsBuilder> configureContext,
            CheapContextOptions? contextOptions = null)
            where TUser : IdentityUser
        {
            var options = contextOptions ?? new CheapContextOptions();

            // Register context options
            services.AddSingleton(options);

            // Add DbContext
            services.AddDbContext<CheapContext<TUser>>(configureContext);

            return new CheapContextBuilder<TUser>(services, options);
        }

        /// <summary>
        /// Adds CheapContext with IdentityUser. Chain .AddIdentity() for Identity services.
        /// </summary>
        public static CheapContextBuilder<IdentityUser> AddCheapContext(
            this IServiceCollection services,
            Action<DbContextOptionsBuilder> configureContext,
            CheapContextOptions? contextOptions = null)
        {
            return services.AddCheapContext<IdentityUser>(configureContext, contextOptions);
        }
    }

    /// <summary>
    /// Builder for fluent CheapContext configuration
    /// </summary>
    public class CheapContextBuilder<TUser> where TUser : IdentityUser
    {
        private readonly IServiceCollection _services;
        private readonly CheapContextOptions _contextOptions;

        internal CheapContextBuilder(IServiceCollection services, CheapContextOptions contextOptions)
        {
            _services = services;
            _contextOptions = contextOptions;
        }

        /// <summary>
        /// Adds Identity services with CheapContext defaults. Follows standard .AddIdentity() pattern.
        /// </summary>
        public IdentityBuilder AddIdentity<TRole>(Action<IdentityOptions>? configureOptions = null)
            where TRole : IdentityRole
        {
            var identityBuilder = _services.AddIdentity<TUser, TRole>(identityOptions =>
            {
                // Apply CheapContext defaults first
                identityOptions.Password = _contextOptions.Identity.Password;
                identityOptions.SignIn = _contextOptions.Identity.SignIn;
                identityOptions.Lockout = _contextOptions.Identity.Lockout;
                identityOptions.User = _contextOptions.Identity.User;
                identityOptions.Stores = _contextOptions.Identity.Stores;
                identityOptions.Tokens = _contextOptions.Identity.Tokens;
                identityOptions.ClaimsIdentity = _contextOptions.Identity.ClaimsIdentity;

                // Allow user overrides
                configureOptions?.Invoke(identityOptions);
            })
            .AddEntityFrameworkStores<CheapContext<TUser>>()
            .AddDefaultTokenProviders();

            return identityBuilder;
        }

        /// <summary>
        /// Adds Identity services with IdentityRole and CheapContext defaults.
        /// </summary>
        public IdentityBuilder AddIdentity(Action<IdentityOptions>? configureOptions = null)
        {
            return AddIdentity<IdentityRole>(configureOptions);
        }

        /// <summary>
        /// Access to the underlying service collection for additional configuration.
        /// </summary>
        public IServiceCollection Services => _services;
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