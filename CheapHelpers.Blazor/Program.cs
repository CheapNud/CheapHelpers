//using Microsoft.AspNetCore.Hosting;
//using Microsoft.Extensions.Hosting;

//namespace MecamApplication.Blazor
//{
//    public class Program
//    {
//        public static string[] SupportedCultures => ["nl-BE", "nl-NL", "en-BE", "en-NL", "fr-BE"];  //needs to be configured in the appsettings.json or overriden or something

//        public static readonly string[] _developers =
//            [
//                "exmaple@axemple.be", //needs to be configured in the appsettings.json or overriden or something
//            ];

//#pragma warning disable CA2211
//        public static string DefaultAccount { get; set; }
//#pragma warning restore CA2211

//        public static void Main(string[] args)
//        {
//            CreateHostBuilder(args).Build().Run();
//        }

//        public static IHostBuilder CreateHostBuilder(string[] args) =>
//            Host.CreateDefaultBuilder(args)
//                .ConfigureWebHostDefaults(webBuilder =>
//                {
//                    webBuilder.UseStartup<Startup>();
//                });
//    }
//}
