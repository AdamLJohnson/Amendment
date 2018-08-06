using System;
using System.Data;
using System.Threading.Tasks;
using Amendment.Model.DataModel;
using Amendment.Repository;
using Amendment.Web.IdentityStores;
using Amendment.Web.IoC;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;

namespace Amendment.Web
{
    public abstract class StartupBase
    {
        public IContainer ApplicationContainer { get; private set; }
        public IConfiguration Configuration { get; }

        protected abstract DbContextOptions<AmendmentContext> ConfigureContextOptions(IServiceProvider serviceProvider);
        protected abstract void SetupEnvSpecificServices(IServiceCollection services);

        protected StartupBase(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            //services.AddDbContext<ApplicationDbContext>(options =>
            //    options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.AddSingleton(ConfigureContextOptions);

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

            services.AddTransient<IUserStore<User>, UserStore>();

            // Add application services.
            //https://docs.microsoft.com/en-us/aspnet/core/security/authentication/identity-custom-storage-providers?view=aspnetcore-2.1


            services.AddMvc();

            builder.Populate(services);
            this.ApplicationContainer = builder.Build();

            //var hasher = ApplicationContainer.Resolve<IPasswordHasher>();
            //AutoMapperConfiguration.Configure(hasher);

            // Create the IServiceProvider based on the container.
            return new AutofacServiceProvider(this.ApplicationContainer);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public async void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                var seeder = new SeedDatabase(app.ApplicationServices);
                await seeder.Seed();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "areas",
                    template: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
                );

                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}