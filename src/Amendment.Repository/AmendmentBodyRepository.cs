using System;
using System.Collections.Generic;
using System.Text;
using Amendment.Model.DataModel;
using Amendment.Repository.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Amendment.Repository
{
    public interface IAmendmentBodyRepository : IRepository<AmendmentBody>
    {

    }

    public class AmendmentBodyRepository : BaseRepository<AmendmentBody>, IAmendmentBodyRepository
    {
        public AmendmentBodyRepository(IDbFactory dbFactory) : base(dbFactory)
        {
            Query = DbSet
                .Include(e => e.Language);
        }
    }
}
