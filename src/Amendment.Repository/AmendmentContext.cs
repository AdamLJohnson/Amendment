using System;
using System.Collections.Generic;
using System.Text;
using Amendment.Model.DataModel;
using Microsoft.EntityFrameworkCore;

namespace Amendment.Repository
{
    public class AmendmentContext : DbContext
    {
        public readonly bool HasConnection; //has connection to database provider
        public AmendmentContext(DbContextOptions<AmendmentContext> options)
            : base(options)
        {
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
                .HasData(new User(){
                    Id = 1,
                    Username = "admin",
                    Email = "admin@admin.com",
                    Name = "Admin",
                    Password = "$2b$12$HbvEC6UaeXiGGlv8ztvzL.SB6oBXKi2QoBkJsjwQvDJGpQ59CmWrq",
                    EnteredBy = -1,
                    EnteredDate = DateTime.UtcNow,
                    LastUpdatedBy = -1,
                    LastUpdated = DateTime.UtcNow,
                });

            builder.Entity<Role>()
                .HasData(new Role()
                {
                    Id = 1,
                    Name = "System Administrator",
                    EnteredBy = -1,
                    EnteredDate = DateTime.UtcNow,
                    LastUpdatedBy = -1,
                    LastUpdated = DateTime.UtcNow,
                },
                new Role()
                {
                    Id = 2,
                    Name = "Screen Controller",
                    EnteredBy = -1,
                    EnteredDate = DateTime.UtcNow,
                    LastUpdatedBy = -1,
                    LastUpdated = DateTime.UtcNow,
                },
                new Role()
                {
                    Id = 3,
                    Name = "Amendment Editor",
                    EnteredBy = -1,
                    EnteredDate = DateTime.UtcNow,
                    LastUpdatedBy = -1,
                    LastUpdated = DateTime.UtcNow,
                },
                new Role()
                {
                    Id = 4,
                    Name = "Translator",
                    EnteredBy = -1,
                    EnteredDate = DateTime.UtcNow,
                    LastUpdatedBy = -1,
                    LastUpdated = DateTime.UtcNow,
                });
            builder.Entity<UserXRole>()
                .HasOne(u => u.User)
                .WithMany(r => r.UserXRoles)
                .HasForeignKey(ur => ur.UserId);

            builder.Entity<UserXRole>()
                .HasOne(r => r.Role)
                .WithMany(r => r.UserXRoles)
                .HasForeignKey(r => r.RoleId);

            builder.Entity<UserXRole>()
                .HasKey(x => new {x.UserId, x.RoleId});
            builder.Entity<UserXRole>()
                .HasData(new UserXRole() {UserId = 1, RoleId = 1});

            builder.Entity<Model.DataModel.Amendment>();
            builder.Entity<AmendmentBody>()
                .HasIndex(x => new {x.AmendId, x.LanguageId});

            builder.Entity<Language>()
                .HasData(new Language() { Id = 1, LanguageName = "English", LanguageCode = "ENG" }
                , new Language() { Id = 2, LanguageName = "Spanish", LanguageCode = "SPA" }
                , new Language() { Id = 3, LanguageName = "French", LanguageCode = "FRA" });
        }
    }
}
