using System.Diagnostics;
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
using Amendment.Repository;
using Amendment.Server.Services;
using Amendment.Shared.Interfaces;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Amendment
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddHttpLogging(o =>
            {
                o.LoggingFields = HttpLoggingFields.RequestHeaders | HttpLoggingFields.ResponseHeaders | HttpLoggingFields.Duration | HttpLoggingFields.RequestMethod | HttpLoggingFields.RequestPath | HttpLoggingFields.ResponseStatusCode;
                o.CombineLogs = true;
            });
            builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
            builder.Host.ConfigureContainer<ContainerBuilder>(builder => builder.RegisterModule(new RegisterDataServices()));
            //builder.Host.ConfigureContainer<ContainerBuilder>(builder => builder.RegisterModule<RegisterMapperProfile>());
            builder.Services.AddResponseCompression(opts =>
            {
                opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    new[]
                    {
                        "application/octet-stream", 
                        "image/png", 
                        "text/javascript",
                        "text/css",
                        "application/json"
                    });
            });



            if (builder.Configuration["DbType"] == "DB")
            {
                builder.Services.AddDbContext<Repository.AmendmentContext>(options =>
                {
                    // DOCKER COMMAND: docker run -p 5432:5432 -e POSTGRES_PASSWORD=mysecretpassword -e POSTGRES_USER=amendment -d postgres
                    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
                });
            }
            else
            {
                builder.Services.AddDbContext<Repository.AmendmentContext>(options =>
                {
                    options.UseInMemoryDatabase("Amendment");
                    options.ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning));
                });
            }

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
                            (path.StartsWithSegments("/amendmentHub") || path.StartsWithSegments("/timerHub")))
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

            var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
            if (string.IsNullOrEmpty(redisConnectionString))
            {
                builder.Services.AddSignalR(options =>
                {
                    options.EnableDetailedErrors = true;
                });
            }
            else
            {
                builder.Services.AddSignalR(options =>
                {
                    options.EnableDetailedErrors = true;
                }).AddStackExchangeRedis(redisConnectionString);
            }



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
            builder.Services.AddScoped<IClientNotifier, Amendment.Server.Services.ClientNotifier>();
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddSingleton<ITimerService, TimerService>();
            
            var app = builder.Build();
            //app.UseResponseCompression();

            app.Use(async (context, next) =>
            {
                var hostname = Environment.GetEnvironmentVariable("HOSTNAME") ?? Environment.GetEnvironmentVariable("COMPUTERNAME") ?? "";
                if (!string.IsNullOrEmpty(hostname))
                {
                    context.Response.Headers.TryAdd("Pod", hostname);
                }
                await next();
            });

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                // app.UseDeveloperExceptionPage();
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
                if (builder.Configuration["DbType"] == "DB")
                    await context.Database.MigrateAsync();
                else
                    await context.Database.EnsureCreatedAsync();
                //DbInitializer.Initialize(context);
                var seeder = new SeedDatabase(app.Services);
                await seeder.Seed();

            }

            //app.UseHttpsRedirection();

            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            //app.UseHttpLogging();
            app.Use(async (context, next) =>
            {
                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                if(context.Request.Path.Value == "/amendmentHub" || context.Request.Path.Value == "/screenHub" || context.Request.Path.Value == "/timerHub")
                    logger.LogInformation("{0}: {1}", context.Request.Method, context.Request.Path);
                var sw = Stopwatch.StartNew();
                await next();
                if (context.Request.Path.Value != "/amendmentHub" && context.Request.Path.Value != "/screenHub" && context.Request.Path.Value != "/timerHub")
                    logger.LogInformation("{0}: {1} {2} {3}ms", context.Request.Method, context.Request.Path, context.Response.StatusCode, sw.ElapsedMilliseconds);
            });

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapRazorPages();
            app.MapControllers();

            app.MapHub<AmendmentHub>("/amendmentHub");
            app.MapHub<ScreenHub>("/screenHub");
            app.MapHub<TimerHub>("/timerHub");

            app.MapFallbackToFile("index.html");

            app.Run();
        }
    }
}