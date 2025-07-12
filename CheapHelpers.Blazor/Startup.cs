//using Azure;
//using BlazorDownloadFile;
//using CheapHelpers.Blazor.Shared;
//using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.Components.Authorization;
//using Microsoft.AspNetCore.Components.Server;
//using Microsoft.AspNetCore.Hosting;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.HttpOverrides;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.ResponseCompression;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Azure;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using MudBlazor;
//using MudBlazor.Services;
//using Newtonsoft.Json;
//using System;
//using System.Diagnostics;
//using System.Linq;
//using System.Threading.Tasks;

//namespace CheapHelpers.Blazor
//{
//    public class Startup
//    {
//        public Startup(IConfiguration configuration, IWebHostEnvironment env)
//        {
//            Configuration = configuration;
//            Program.DefaultAccount = Configuration["UserEmail"];
//            Env = env;
//        }

//        public IConfiguration Configuration { get; }
//        public static IWebHostEnvironment Env { get; private set; }

//        // This method gets called by the runtime. Use this method to add services to the container.
//        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
//        public void ConfigureServices(IServiceCollection services)
//        {
//            Debug.WriteLine("Configure Services");
//            services.AddScoped<IEmailService, GraphService>(
//                x =>
//                    new GraphService(
//                        "Mecam",
//                        "noreply@mecam.be",
//                        Configuration["ClientId"],
//                        Configuration["TenantId"],
//                        Configuration["ClientSecret"],
//                        Env.IsDevelopment(),
//                        Program._developers
//                    )
//            );
//            services.AddLocalization();
//            services.AddDbContextFactory<MecamContext>(options =>
//            {
//                var con = Configuration.GetConnectionString("AzureSQLConnection");
//                Debug.WriteLine(con);
//                options.UseSqlServer(con, x => x.MigrationsAssembly("MecamApplication.Migrations"));
//            } );
//            services
//                .AddIdentity<ApplicationUser, IdentityRole>(options =>
//                {
//                    // Password settings.
//                    options.Password.RequireDigit = true;
//                    options.Password.RequireLowercase = true;
//                    options.Password.RequireNonAlphanumeric = false;
//                    options.Password.RequireUppercase = true;
//                    options.Password.RequiredLength = 8;
//                    options.Password.RequiredUniqueChars = 1;

//                    //signin settings
//                    options.SignIn.RequireConfirmedAccount = false;

//                    // Lockout settings.
//                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
//                    options.Lockout.MaxFailedAccessAttempts = 8;
//                    options.Lockout.AllowedForNewUsers = true;

//                    // User settings.
//                    options.User.AllowedUserNameCharacters =
//                        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
//                    options.User.RequireUniqueEmail = true;
//                })
//                .AddEntityFrameworkStores<MecamContext>()
//                .AddDefaultTokenProviders();


//            //.AddDefaultUI();

//            //services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
//            //    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
//            //{
//            //    options.Cookie.Name = "MecamCookie";
//            //    options.LoginPath = "/account/login";
//            //options.Cookie.SameSite = SameSiteMode.Lax;
//            //    options.Cookie.HttpOnly = true;
//            //});

//            services.AddRazorPages();

//            services.AddServerSideBlazor(options =>
//            {
//                options.DetailedErrors = true;
//                options.DisconnectedCircuitMaxRetained = 100;
//                options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);

//                // Don't set JSInteropDefaultCallTimeout here as it might conflict
//                // with custom boot.js reconnection handler timeouts

//                options.MaxBufferedUnacknowledgedRenderBatches = 20;
//            });

//            services.AddScoped<
//                AuthenticationStateProvider,
//                RevalidatingIdentityAuthenticationStateProvider<ApplicationUser>
//            >();

//            services.AddScoped<IHostEnvironmentAuthenticationStateProvider>(
//                sp =>
//                    (ServerAuthenticationStateProvider)
//                        sp.GetRequiredService<AuthenticationStateProvider>()
//            );

//            if (Env.IsDevelopment())
//            {
//                services.AddDatabaseDeveloperPageExceptionFilter();
//            }

//            //policies
//            //services.AddAuthorizationBuilder()
//            //    .AddPolicy(Policies.Admin, policy => policy.RequireRole(Roles.Admin))
//            //    .AddPolicy(Policies.ServiceExternal, policy => policy.RequireRole(
//            //                Roles.Admin,
//            //                Roles.ServiceAdmin,
//            //                Roles.ServiceUser,
//            //                Roles.ServiceExternal
//            //            ))
//            //    .AddPolicy(Policies.ServiceUser, policy =>
//            //            policy.RequireRole(
//            //                Roles.Admin,
//            //                Roles.ServiceAdmin,
//            //                Roles.ServiceUser
//            //            ))
//            //    .AddPolicy(Policies.ServiceMechanic, policy =>
//            //            policy.RequireRole(
//            //                Roles.Admin,
//            //                Roles.ServiceAdmin,
//            //                Roles.ServiceUser,
//            //                Roles.ServiceMechanic
//            //            ))
//            //    .AddPolicy(Policies.ServiceAdmin, policy =>
//            //            policy.RequireRole(
//            //                Roles.Admin,
//            //                Roles.ServiceAdmin
//            //            ))
//            //    .AddPolicy(Policies.ServiceSupplier, policy =>
//            //            policy.RequireRole(
//            //                Roles.Admin,
//            //                Roles.ServiceSupplier,
//            //                Roles.ServiceUser,
//            //                Roles.ServiceAdmin
//            //            ))
//            //    .AddPolicy(Policies.Service, policy =>
//            //            policy.RequireRole(
//            //                Roles.Admin,
//            //                Roles.ServiceSupplier,
//            //                Roles.ServiceAdmin,
//            //                Roles.ServiceMechanic,
//            //                Roles.ServiceUser,
//            //                Roles.ServiceExternal
//            //            ))
//            //    .AddPolicy(Policies.ServiceEmployee, policy =>
//            //            policy.RequireRole(
//            //                Roles.Admin,
//            //                Roles.ServiceSupplier,
//            //                Roles.ServiceAdmin,
//            //                Roles.ServiceUser,
//            //                Roles.ServiceExternal
//            //            ))
//            //    .AddPolicy(Policies.Representative, policy => policy.RequireRole(
//            //        Roles.Admin,
//            //        Roles.Representative
//            //        ))
//            //    .AddPolicy(Policies.WorkInstructionUser, policy =>
//            //            policy.RequireRole(
//            //                Roles.Admin,
//            //                Roles.WorkInstructionUser,
//            //                Roles.WorkInstructionAdmin
//            //            ))
//            //    .AddPolicy(Policies.WorkInstructionAdmin, policy => policy.RequireRole(Roles.Admin, Roles.WorkInstructionAdmin))
//            //    .AddPolicy(Policies.Expedition, policy => policy.RequireRole(Roles.Admin, Roles.Expedition))
//            //    .AddPolicy(Policies.QualityUser, policy => policy.RequireRole(Roles.Admin, Roles.QualityUser, Roles.QualityAdmin))
//            //    .AddPolicy(Policies.QualityUserWatex, policy =>
//            //            policy.RequireRole(
//            //                Roles.Admin,
//            //                Roles.QualityUser,
//            //                Roles.QualityAdmin,
//            //                Roles.QualityUserWatex
//            //            ))
//            //    .AddPolicy(Policies.QualityAdmin, policy => policy.RequireRole(Roles.Admin, Roles.QualityAdmin))
//            //    .AddPolicy(Policies.ModelAdmin, policy => policy.RequireRole(Roles.Admin, Roles.ModelAdmin))
//            //    .AddPolicy(Policies.ModelUser, policy => policy.RequireRole(Roles.Admin, Roles.ModelUser, Roles.ModelAdmin))
//            //    .AddPolicy(Policies.StockExternal, policy =>
//            //            policy.RequireRole(
//            //                Roles.Admin,
//            //                Roles.StockExternal,
//            //                Roles.SalesUser,
//            //                Roles.SalesAdmin
//            //            ))
//            //    .AddPolicy(Policies.Customer, policy =>
//            //            policy.RequireRole(
//            //                Roles.Admin,
//            //                Roles.Customer,
//            //                Roles.SalesAdmin
//            //            ))
//            //    .AddPolicy(Policies.SalesUser, policy => policy.RequireRole(
//            //            Roles.Admin,
//            //            Roles.SalesUser,
//            //            Roles.SalesAdmin
//            //        ))
//            //    .AddPolicy(Policies.Sales, policy => policy.RequireRole(
//            //            Roles.Admin,
//            //            Roles.SalesUser,
//            //            Roles.SalesAdmin,
//            //            Roles.StockExternal,
//            //            Roles.Customer
//            //            ))
//            //    .AddPolicy(Policies.SalesAdmin, policy => policy.RequireRole(Roles.Admin, Roles.SalesAdmin))
//            //    .AddPolicy(Policies.PurchasingUser, policy => policy.RequireRole(
//            //            Roles.Admin,
//            //            Roles.PurchasingUser,
//            //            Roles.PurchasingAdmin
//            //            ))
//            //    .AddPolicy(Policies.PurchasingAdmin, policy => policy.RequireRole(
//            //        Roles.Admin,
//            //        Roles.PurchasingAdmin
//            //        ))
//            //    .AddPolicy(Policies.HumanResources, policy => policy.RequireRole(
//            //        Roles.Admin,
//            //        Roles.HumanResources
//            //        ))
//            //    .AddPolicy(Policies.Xcalibur, policy => policy.RequireRole(
//            //        Roles.Admin,
//            //        Roles.Xcalibur
//            //        ))
//            //    .AddPolicy(Policies.Production, policy =>
//            //            policy.RequireRole(
//            //                Roles.Admin,
//            //                Roles.Expedition,
//            //                Roles.QualityUserWatex,
//            //                Roles.WorkInstructionUser,
//            //                Roles.WorkInstructionAdmin,
//            //                Roles.QualityAdmin,
//            //                Roles.QualityUser
//            //            ))
//            //    .AddPolicy(Policies.AccountancyUser, policy => policy.RequireRole(
//            //        Roles.Admin,
//            //        Roles.AccountancyUser,
//            //        Roles.AccountancyAdmin
//            //        ))
//            //    .AddPolicy(Policies.AccountancyAdmin, policy => policy.RequireRole(
//            //        Roles.Admin,
//            //        Roles.AccountancyAdmin
//            //        ))
//            //    .AddPolicy(Policies.Purchasing, policy => policy.RequireRole(
//            //        Roles.Admin,
//            //        Roles.PurchasingAdmin,
//            //        Roles.PurchasingUser,
//            //        Roles.PurchasingModelAdmin
//            //        ))
//            //    .AddPolicy(Policies.Accountancy, policy => policy.RequireRole(
//            //        Roles.Admin,
//            //        Roles.AccountancyAdmin,
//            //        Roles.AccountancyUser
//            //        ))
//            //    .AddPolicy(Policies.ModelUser, policy => policy.RequireRole(
//            //        Roles.Admin,
//            //        Roles.ModelAdmin,
//            //        Roles.ModelUser
//            //        ))
//            //    .AddPolicy(Policies.PurchasingModelAdmin, policy => policy.RequireRole(
//            //        Roles.Admin,
//            //        Roles.PurchasingAdmin,
//            //        Roles.PurchasingModelAdmin
//            //        ));

//            services.Configure<CookiePolicyOptions>(options =>
//            {
//                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
//                options.CheckConsentNeeded = context => false;
//                options.MinimumSameSitePolicy = SameSiteMode.None;
//            });

//            services.AddAzureClients(
//                builder =>
//                    builder.AddBlobServiceClient(
//                        Configuration.GetConnectionString("StorageConnection")
//                    )
//            );

//            //CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Configuration.GetConnectionString("StorageConnection"));
//            //CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

//            //services.AddScoped<VisionServiceOptions>(
//            //    x =>
//            //        new(
//            //            Configuration["VisionEndpoint"],
//            //            new AzureKeyCredential(Configuration["VisionKey"])
//            //        )
//            //);

//            services.AddScoped<IBarcodeService, BarcodeService>();
//            services.AddScoped<ISmsService, TwilioSmsService>();
//            services.AddScoped<ICsvService, CsvService>();
//            services.AddScoped<IPdfService, MecamApplication.CoreStandard.Services.PdfTextService>();
//            services.AddScoped<IXlsxService, XlsxService>();
//            services.AddScoped<IXmlService, XmlService>();
//            services.AddScoped<TranslatorService>(
//                x =>
//                    new(
//                        Configuration["TranslationKey"],
//                        Configuration["TranslationEndpoint"],
//                        Configuration["TranslationDocumentEndpoint"]
//                    )
//            );
//            services.AddScoped<AddressSearchService>(
//                x =>
//                    new(
//                        Configuration["MapsKey"],
//                        Configuration["MapsClientId"],
//                        Configuration["MapsEndpoint"]
//                    )
//            );

//            services.AddScoped<BaseRepo>();
//            //services.AddScoped<CloudBlobClient>(x => blobClient);
//            services.AddScoped<BlobService>();
//            services.AddScoped<CookieProvider>();
//            services.AddScoped<ClipboardService>();

//            //do not use singletons!!!! you will leak claims across instances if you do not know what you're doing
//            //services.AddSingleton<ConnectedUserList>();
//            services.AddScoped<UserRepo>();
//            services.AddScoped<ServiceRepo>();
//            services.AddScoped<CustomerRepo>();
//            services.AddScoped<OrderTypeRepo>();
//            services.AddScoped<PricelistRepo>();
//            services.AddScoped<CollectionRepo>();
//            services.AddScoped<ElementOptionRepo>();
//            services.AddScoped<ModelRepo>();
//            services.AddScoped<WorkInstructionRepo>();
//            services.AddScoped<SupplementRepo>();
//            services.AddScoped<FabricTypeRepo>();
//            services.AddScoped<WoodColorGroupRepo>();
//            services.AddScoped<CreditNoteRepo>();
//            services.AddScoped<OrderRepo>();
//            services.AddScoped<ClientGroupRepo>();
//            services.AddScoped<DownloadHelper>();
//            services.AddScoped<UblService>();
//            services.AddScoped<UserService>();
//            services.AddScoped<CustomNavigationService>();
//            services.AddScoped<IbanValidator>();
//            services.AddScoped<PurchasingRepo>();
//            services.AddScoped<ArticleRepo>();

//            services.AddAutoMapper(typeof(MappingProfile));

//            services.AddMudServices(config =>
//            {
//                config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;
//                config.SnackbarConfiguration.PreventDuplicates = true;
//                config.SnackbarConfiguration.NewestOnTop = false;
//                config.SnackbarConfiguration.ShowCloseIcon = true;
//                config.SnackbarConfiguration.MaxDisplayedSnackbars = 3;
//                config.SnackbarConfiguration.VisibleStateDuration = 1500;
//                config.SnackbarConfiguration.HideTransitionDuration = 300;
//                config.SnackbarConfiguration.ShowTransitionDuration = 300;
//                config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
//            });

//            services.AddLocalization();
//            services.AddBlazorDownloadFile();

//            //services.ConfigureHttpJsonOptions(options =>
//            //{
//            //    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
//            //    options.SerializerOptions.MaxDepth = 264;
//            //    //options.SerializerOptions.AllowTrailingCommas = true;
//            //});

//            services.AddControllers().AddJsonOptions(options =>
//            {
//                options.JsonSerializerOptions.MaxDepth = 264;
//                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;

//                //options.SerializerOptions.AllowTrailingCommas = true;
//            });

//            //services.AddControllers().AddNewtonsoftJson(options => options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);

//            JsonConvert.DefaultSettings = () =>
//                new JsonSerializerSettings
//                {
//                    Formatting = Newtonsoft.Json.Formatting.None,
//                    ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
//                };

//            services.AddResponseCompression(options =>
//            {
//                options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
//                    [MimeTypes.Application.OctetStream]
//                );
//            });

//            services.AddHsts(options =>
//            {
//                options.Preload = true;
//                options.IncludeSubDomains = true;
//                options.MaxAge = TimeSpan.FromDays(365);
//            });

//            //services.AddNewtonSoftJson();

//            //services.AddRouting(options =>
//            //{
//            //    options.ConstraintMap.Add("encrypted", typeof(EncryptedRouteConstraint));
//            //});

//            services.AddHttpsRedirection(options =>
//            {
//                options.HttpsPort = 443; //default port redirection
//            });
//        }

//        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
//        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
//        {
//            Debug.WriteLine(env.EnvironmentName);
//            app.UseHttpsRedirection();
//            //app.UseRewriter(new RewriteOptions().AddRedirectToWwwPermanent());

//            app.Use(
//                async (context, next) =>
//                {
//                    //CultureInfo.CurrentCulture = CultureInfo.CreateSpecificCulture("nl-BE");
//                    //CultureInfo.CurrentUICulture = CultureInfo.CreateSpecificCulture("nl-BE");
//                    //CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CreateSpecificCulture("nl-BE");
//                    //CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.CreateSpecificCulture("nl-BE");

//                    //uncomment this is to force udpate js
//                    context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
//                    context.Response.Headers.Append("Pragma", "no-cache");
//                    context.Response.Headers.Append("Expires", "0");

//                    // Call the next delegate/middleware in the pipeline
//                    await next(context);
//                }
//            );

//            //forwards client information (eg IP address) from the loadbalancer to the app
//            app.UseForwardedHeaders(
//                new ForwardedHeadersOptions
//                {
//                    ForwardedHeaders =
//                        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
//                }
//            );

//            if (env.IsDevelopment())
//            {
//                app.UseDeveloperExceptionPage();
//                //app.UseMigrationsEndPoint();
//            }
//            else
//            {
//                app.UseExceptionHandler("/Error");
//                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//                app.UseHsts();
//            }

//            app.UseStaticFiles();

//            var localizationOptions = new RequestLocalizationOptions()
//                .SetDefaultCulture(Program.SupportedCultures.First())
//                .AddSupportedCultures(Program.SupportedCultures)
//                .AddSupportedUICultures(Program.SupportedCultures);

//            app.UseRequestLocalization(localizationOptions);
//            app.UseRouting();

//            // app.UseCookiePolicy();

//            app.UseAuthentication();
//            app.UseAuthorization();

//            app.UseResponseCompression();

//            app.UseEndpoints(endpoints =>
//            {
//                endpoints.MapControllers();
//                endpoints.MapBlazorHub(options =>
//                {
//                    //this configures the timeout, default 5
//                    options.WebSockets.CloseTimeout = TimeSpan.FromSeconds(8);
//                    options.AllowStatefulReconnects = true;
//                    //should default to 90secs
//                    //options.LongPolling.PollTimeout = TimeSpan.FromHours(1);
//                });
//                endpoints.MapHub<ChatHub>(@"/hub/chat");
//                endpoints.MapRazorPages();
//                endpoints.MapFallbackToPage("/_Host");
//            });

//            Debug.WriteLine("Creating roles...");
//            CreateRoles(app).Wait();
//        }

//        private async Task CreateRoles(IApplicationBuilder app)
//        {
//            //initializing custom roles
//            using IServiceScope scope = app.ApplicationServices.CreateScope();
//            RoleManager<IdentityRole> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
//            UserManager<ApplicationUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
//            // Seed database code goes here
//            IdentityResult roleResult;

//            foreach (var roleName in Roles.GetAll())
//            {
//                bool roleExist = await roleManager.RoleExistsAsync(roleName);
//                if (!roleExist)
//                {
//                    roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
//                }
//            }

//            ApplicationUser poweruser =
//                new() { UserName = Configuration["UserEmail"], Email = Configuration["UserEmail"] };

//            //Ensure you have these values in your appsettings.json file
//            string password = Configuration["UserPassword"];
//            var user = await userManager.FindByEmailAsync(Configuration["UserEmail"]);
//            if (user == null)
//            {
//                IdentityResult createPowerUser = await userManager.CreateAsync(poweruser, password);
//                if (createPowerUser.Succeeded)
//                {
//                    //tie the new user to the role
//                    await userManager.AddToRoleAsync(poweruser, Roles.Admin);
//                }
//            }
//        }
//    }
//}
