using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FluentValidation;
using Autofac.Extensions.DependencyInjection;
using Autofac;
using Amendment.Server.IoC;
using Amendment.Server.PipelineBehaviors;
using Amendment.Server.Mediator.Handlers;
using Amendment.Server.Mediator.Commands;
using Amendment.Server;
using Amendment.Server.Hubs;
using Amendment.Service;
using Amendment.Service.Infrastructure;
using Microsoft.AspNetCore.ResponseCompression;
using Autofac.Core;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Amendment
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
            builder.Host.ConfigureContainer<ContainerBuilder>(builder => builder.RegisterModule(new RegisterDataServices()));
            //builder.Host.ConfigureContainer<ContainerBuilder>(builder => builder.RegisterModule<RegisterMapperProfile>());
            builder.Services.AddResponseCompression(opts =>
            {
                opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    new[] { "application/octet-stream" });
            });

            builder.Services.AddDbContext<Repository.AmendmentContext>(options =>
            {
                //options.UseSqlite("Filename=:memory:");
                options.UseInMemoryDatabase("Amendment");
                options.ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });

            var jwtSettings = builder.Configuration.GetSection("JWTSettings");
            builder.Services.AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = jwtSettings["validIssuer"],
                    ValidAudience = jwtSettings["validAudience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["securityKey"] ?? throw new NullReferenceException("Security key can not be null")))
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];

                        // If the request is for our hub...
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) &&
                            (path.StartsWithSegments("/amendmentHub")))
                        {
                            // Read the token out of the query string
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();
            builder.Services.AddSignalR();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddMediatR(configuration =>
            {
                configuration.RegisterServicesFromAssembly(typeof(AccountLoginHandler).Assembly);
            });
            builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<>));
            builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ClientNotificationBehavior<,>));
            builder.Services.AddValidatorsFromAssembly(typeof(AccountLoginCommand).Assembly);
            builder.Services.RegisterMapsterConfiguration();
            builder.Services.AddSingleton<IClientNotifier, MockClientNotifier>();
            builder.Services.AddScoped<ITokenService, TokenService>();

            var app = builder.Build();
            //app.UseResponseCompression();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseResponseCompression();
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                var context = services.GetRequiredService<Repository.AmendmentContext>();
                context.Database.EnsureCreated();
                //DbInitializer.Initialize(context);
#if DEBUG
                var seeder = new SeedDatabase(app.Services);
                await seeder.Seed();
#endif
            }

            app.UseHttpsRedirection();

            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapRazorPages();
            app.MapControllers();

            app.MapHub<AmendmentHub>("/amendmentHub");
            app.MapHub<ScreenHub>("/screenHub");
            //app.MapHub<DiffHub>("/diffHub");

            app.MapFallbackToFile("index.html");

            app.Run();
        }
    }
}