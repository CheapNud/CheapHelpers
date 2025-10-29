using CheapHelpers;

namespace CheapHelpers.EF.Infrastructure
{
    public class CheapSeedOptions
    {
        public string AdminEmailConfigKey { get; set; } = Constants.Configuration.UserEmail;
        public string AdminPasswordConfigKey { get; set; } = Constants.Configuration.UserPassword;
        public string AdminRoleName { get; set; } = Constants.Authentication.AdminRole;
        public string[] DefaultRoles { get; set; } = [Constants.Authentication.AdminRole, Constants.Authentication.UserRole];
        public bool CreateDefaultAdmin { get; set; } = true;
        public bool SeedRoles { get; set; } = true;
    }
}

// Usage Examples:
//
// Program.cs:
// var builder = WebApplication.CreateBuilder(args);
// 
// builder.Services.AddCheapContext<ApplicationUser>(options => options.UseSqlServer(connectionString))
//     .AddIdentity<IdentityRole>();
// 
// var app = builder.Build();
// 
// // Seed database
// await app.Services.SeedCheapContextAsync<ApplicationUser, IdentityRole>(new CheapSeedOptions
// {
//     DefaultRoles = ["Admin", "Manager", "User"],
//     AdminRoleName = "Admin"
// });
// 
// app.Run();
//
// Or simple version:
// await app.Services.SeedCheapContextAsync(); // Uses defaults
//
// appsettings.json:
// {
//   "UserEmail": "admin@yourapp.com",
//   "UserPassword": "Admin123!",
//   "ConnectionStrings": {
//     "DefaultConnection": "..."
//   }
// }