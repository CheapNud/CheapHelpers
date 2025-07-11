using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CheapHelpers.EF
{
    public class CheapContext : IdentityDbContext<IdentityUser>
    {
        private static bool IsInDev()
        {
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.ToLower() ?? "production";
            return environmentName.Equals("development");
        }

        public string ConnectionString => this.Database.GetConnectionString();

        public CheapContext()
        {

        }

        public CheapContext(DbContextOptions<CheapContext> options) : base(options)
        {

            if (IsInDev())
            {
                Database.SetCommandTimeout(150000);
            }

            //Console.WriteLine($"{Environment.NewLine}Connectionstring: {Database?.GetDbConnection()?.ConnectionString}{Environment.NewLine}");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (IsInDev())
            {
                options.EnableSensitiveDataLogging();
            }

            options.ConfigureWarnings(x => x.Ignore(CoreEventId.RowLimitingOperationWithoutOrderByWarning));
            options.ConfigureWarnings(x => x.Ignore(RelationalEventId.MultipleCollectionIncludeWarning));



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


        //public DbSet<Service> Services { get; set; }



    }
}
