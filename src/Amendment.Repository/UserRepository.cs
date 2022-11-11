using System;
using System.Collections.Generic;
using System.Text;
using Amendment.Model.DataModel;
using Amendment.Repository.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Amendment.Repository
{
    public class UserRepository : BaseRepository<User>
    {
        public UserRepository(IDbFactory dbFactory) : base(dbFactory)
        {
            Query = DbSet.Include(u => u.Roles);
        }
    }
}
