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
            builder.Entity<User>();
            builder.Entity<Role>();
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

            builder.Entity<Model.DataModel.Amendment>();
            builder.Entity<AmendmentBody>()
                .HasIndex(x => new {x.AmendId, x.LanguageId});
        }
    }
}
