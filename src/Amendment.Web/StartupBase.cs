using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Amendment.Model.DataModel;
using Amendment.Model.Infrastructure;
using Amendment.Repository;
using Amendment.Repository.Infrastructure;
using Amendment.Service.Infrastructure;
using Amendment.Web.Hubs;
using Amendment.Web.IdentityStores;
using Amendment.Web.IoC;
using Amendment.Web.Notifiers;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
//using MySql.Data.MySqlClient;

namespace Amendment.Web
{
    public abstract class StartupBase
    {
        public IContainer ApplicationContainer { get; private set; }
        public IConfiguration Configuration { get; }

        protected abstract DbContextOptions<AmendmentContext> ConfigureContextOptions(IServiceProvider serviceProvider);
        protected abstract void SetupEnvSpecificServices(IServiceCollection services);

        protected StartupBase(IHostingEnvironment env)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            //services.AddDbContext<ApplicationDbContext>(options =>
            //    options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.AddSingleton(ConfigureContextOptions);
            services.AddMemoryCache();

            var builder = new ContainerBuilder();
            builder.RegisterModule<RegisterDataServices>();
            builder.RegisterModule<RegisterMapperProfile>();

            services.AddTransient<IUserStore<User>, UserStore>();
            services.AddTransient<IRoleStore<Role>, RoleStore>();
            services.AddScoped<IPasswordHasher<User>, IdentityStores.PasswordHasher<User>>();

            services.AddIdentity<User, Role>()
                .AddUserManager<UserManager<User>>()
                .AddSignInManager<SignInManager<User>>()
                .AddRoleManager<AspNetRoleManager<Role>>()
                .AddDefaultTokenProviders();

            services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.FromDays(30));
            services.AddAuthentication()
                .Services.ConfigureApplicationCookie(options =>
                {
                    options.SlidingExpiration = true;
                    options.ExpireTimeSpan = TimeSpan.FromDays(30);
                });

            services.AddTransient<IUserStore<User>, UserStore>();

            // Add application services.
            //https://docs.microsoft.com/en-us/aspnet/core/security/authentication/identity-custom-storage-providers?view=aspnetcore-2.1

            services.AddSignalR();
            services.AddSingleton<IClientNotifier, ClientNotifier>();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            builder.Populate(services);
            this.ApplicationContainer = builder.Build();

            //var hasher = ApplicationContainer.Resolve<IPasswordHasher>();
            //AutoMapperConfiguration.Configure(hasher);

            // Create the IServiceProvider based on the container.
            return new AutofacServiceProvider(this.ApplicationContainer);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public async void Configure(IApplicationBuilder app, IHostingEnvironment env, IDbFactory dbContext, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                await dbContext.Init().Database.EnsureCreatedAsync();

                var seeder = new SeedDatabase(app.ApplicationServices);
                await seeder.Seed();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseStaticFiles();

            app.UseAuthentication();

            

            var logger = loggerFactory.CreateLogger("Amendment.Web");
            app.Use(LogHttpTraffic(logger));

            //app.UseSignalR(routes =>
            //{
            //    routes.MapHub<AmendmentHub>("/amendmentHub");
            //    routes.MapHub<ScreenHub>("/screenHub");
            //    routes.MapHub<DiffHub>("/diffHub");
            //});

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "areas",
                    template: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
                );

                routes.MapRoute(
                    name: "amendmentBody",
                    template: "amendment/{amendmentId:int}/Body/{action=Index}/{id?}",
                    defaults: new
                    {
                        controller = "AmendmentBody"
                    });

                routes.MapRoute(
                    name: "screenView",
                    template: "View/{id?}",
                    defaults: new
                    {
                        controller = "Home",
                        action = "View"
                    });

                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private static Func<HttpContext, Func<Task>, Task> LogHttpTraffic(ILogger logger)
        {
            return async (context, next) =>
            {
                var sw = Stopwatch.StartNew();

                await next();

                sw.Stop();
                logger.LogInformation("{Method} {Path} {Status} {RequestTime}ms {Username} {RemoteHost}", context.Request.Method, context.Request.Path, context.Response.StatusCode, sw.ElapsedMilliseconds, context.User.Identity.Name, context.Request.HttpContext.Connection.RemoteIpAddress);
            };
        }
    }
}