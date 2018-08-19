using System;
using System.Collections.Generic;
using System.Text;
using Amendment.Repository.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Amendment.Repository
{
    public interface IAmendmentRepository : IRepository<Model.DataModel.Amendment>
    {

    }

    public class AmendmentRepository : BaseRepository<Model.DataModel.Amendment>, IAmendmentRepository
    {
        public AmendmentRepository(IDbFactory dbFactory) : base(dbFactory)
        {
            Query = DbSet.Include(e => e.PrimaryLanguage)
                .Include(e => e.AmendmentBodies)
                .ThenInclude(e => e.Language);
        }
    }
}
