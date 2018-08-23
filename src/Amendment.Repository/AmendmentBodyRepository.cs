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
        void SetIsLive(bool isLive, AmendmentBody amendmentBody);
    }

    public class AmendmentBodyRepository : BaseRepository<AmendmentBody>, IAmendmentBodyRepository
    {
        public AmendmentBodyRepository(IDbFactory dbFactory) : base(dbFactory)
        {
            Query = DbSet
                .Include(e => e.Language);
        }

        public void SetIsLive(bool isLive, AmendmentBody amendmentBody)
        {
            amendmentBody.IsLive = isLive;
            Update(amendmentBody, nameof(AmendmentBody.IsLive));
        }
    }
}
