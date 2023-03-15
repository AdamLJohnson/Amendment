using System;
using System.Collections.Generic;
using System.Text;
using Amendment.Model.DataModel;
using Amendment.Model.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Amendment.Repository
{
    public class AmendmentContext : DbContext
    {
        public readonly bool HasConnection; //has connection to database provider
        public AmendmentContext(DbContextOptions<AmendmentContext> options)
            : base(options)
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            try
            {
                Database.GetDbConnection();
                HasConnection = true;
            }
            catch (Exception)
            {
                HasConnection = false;
            }
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<User>()
                .HasData(new User
                {
                    Id = 1,
                    Username = "admin",
                    Email = "admin@admin.com",
                    Name = "Admin",
                    Password = "$2b$12$HbvEC6UaeXiGGlv8ztvzL.SB6oBXKi2QoBkJsjwQvDJGpQ59CmWrq",
                    EnteredBy = -1,
                    EnteredDate = DateTime.Parse("2018-01-01"),
                    LastUpdatedBy = -1,
                    LastUpdated = DateTime.Parse("2018-01-01"),
                });
            

            builder.Entity<Role>()
                .HasData(
                new Role
                {
                    Id = 1,
                    Name = "System Administrator",
                    EnteredBy = -1,
                    EnteredDate = DateTime.Parse("2018-01-01"),
                    LastUpdatedBy = -1,
                    LastUpdated = DateTime.Parse("2018-01-01")
                },
                new Role()
                {
                    Id = 2,
                    Name = "Screen Controller",
                    EnteredBy = -1,
                    EnteredDate = DateTime.Parse("2018-01-01"),
                    LastUpdatedBy = -1,
                    LastUpdated = DateTime.Parse("2018-01-01"),
                },
                new Role()
                {
                    Id = 3,
                    Name = "Amendment Editor",
                    EnteredBy = -1,
                    EnteredDate = DateTime.Parse("2018-01-01"),
                    LastUpdatedBy = -1,
                    LastUpdated = DateTime.Parse("2018-01-01"),
                },
                new Role()
                {
                    Id = 4,
                    Name = "Translator",
                    EnteredBy = -1,
                    EnteredDate = DateTime.Parse("2018-01-01"),
                    LastUpdatedBy = -1,
                    LastUpdated = DateTime.Parse("2018-01-01"),
                },
                new Role()
                {
                    Id = 5,
                    Name = "Toast Notifications",
                    EnteredBy = -1,
                    EnteredDate = DateTime.Parse("2018-01-01"),
                    LastUpdatedBy = -1,
                    LastUpdated = DateTime.Parse("2018-01-01"),
                });
            //builder.Entity<UserXRole>()
            //    .HasOne(u => u.User)
            //    .WithMany(r => r.UserXRoles)
            //    .HasForeignKey(ur => ur.UserId);

            //builder.Entity<UserXRole>()
            //    .HasOne(r => r.Role)
            //    .WithMany(r => r.UserXRoles)
            //    .HasForeignKey(r => r.RoleId);

            //builder.Entity<UserXRole>()
            //    .HasKey(x => new {x.UserId, x.RoleId});
            //builder.Entity<UserXRole>()
            //    .HasData(new UserXRole() {UserId = 1, RoleId = 1});
            //builder.Entity<UserXRole>()
            //    .HasData(new UserXRole() { UserId = 1, RoleId = 5 });

            //builder.Entity<User>().HasData()

            builder.Entity<User>()
                .HasMany(u => u.Roles)
                .WithMany()
                .UsingEntity(x =>
                {
                    x.ToTable("UserRoles");
                    x.HasData(new { RolesId = 1, UserId = 1 });
                });

            //builder.Entity("UserRoles").HasData(new { RolesId = 1, UsersId = 1 });

            builder.Entity<Model.DataModel.Amendment>();
            builder.Entity<AmendmentBody>()
                .HasIndex(x => new {x.AmendId, x.LanguageId});
            builder.Entity<AmendmentBody>()
                .Property(x => x.Page).HasDefaultValue(0);

            builder.Entity<Language>()
                .HasData(new Language() { Id = 1, LanguageName = "English", LanguageCode = "ENG" }
                , new Language() { Id = 2, LanguageName = "Spanish", LanguageCode = "SPA" }
                , new Language() { Id = 3, LanguageName = "French", LanguageCode = "FRA" });

            builder.Entity<SystemSetting>().HasIndex(x => x.Key).IsUnique();
            builder.Entity<SystemSetting>()
                .HasData(new SystemSetting()
                    {
                        Id = 1,
                        Key = SystemSettingKeys.ShowDeafSigner,
                        Value = "1",
                        EnteredBy = -1,
                        EnteredDate = DateTime.Parse("2018-01-01"),
                        LastUpdatedBy = -1,
                        LastUpdated = DateTime.Parse("2018-01-01")
                },
                    new SystemSetting()
                    {
                        Id = 2,
                        Key = SystemSettingKeys.ShowDeafSignerBox,
                        Value = "1",
                        EnteredBy = -1,
                        EnteredDate = DateTime.Parse("2018-01-01"),
                        LastUpdatedBy = -1,
                        LastUpdated = DateTime.Parse("2018-01-01")
                    },
                    new SystemSetting()
                    {
                        Id = 3,
                        Key = SystemSettingKeys.InvertedSlideText,
                        Value = "1",
                        EnteredBy = -1,
                        EnteredDate = DateTime.Parse("2018-01-01"),
                        LastUpdatedBy = -1,
                        LastUpdated = DateTime.Parse("2018-01-01")
                    });
        }
    }
    public class MyClass : IDesignTimeDbContextFactory<AmendmentContext>
    {
        public AmendmentContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AmendmentContext>();
            //optionsBuilder.UseSqlite("Data Source=blog.db", opt =>
            //{
            //    opt.MigrationsAssembly("Amendment.Server");
            //});
            optionsBuilder.UseNpgsql("Host=localhost;Database=amendment;Username=amendment;Password=mysecretpassword", opt =>
            {
                //opt.MigrationsAssembly("Amendment.Server");
            });

            return new AmendmentContext(optionsBuilder.Options);
        }

    }
}
