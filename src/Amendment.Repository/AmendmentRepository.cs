using System;
using System.Collections.Generic;
using System.Text;
using Amendment.Repository.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Amendment.Repository
{
    public interface IAmendmentRepository : IRepository<Model.DataModel.Amendment>
    {
        void SetIsLive(bool isLive, Model.DataModel.Amendment amendment);
        void SetIsArchived(bool isArchived, Model.DataModel.Amendment amendment);
    }

    public class AmendmentRepository : BaseRepository<Model.DataModel.Amendment>, IAmendmentRepository
    {
        public AmendmentRepository(IDbFactory dbFactory) : base(dbFactory)
        {
            Query = DbSet.Include(e => e.PrimaryLanguage)
                .Include(e => e.ParentAmendment)
                .Include(e => e.AmendmentBodies)
                .ThenInclude(e => e.Language);
        }

        public void SetIsLive(bool isLive, Model.DataModel.Amendment amendment)
        {
            amendment.IsLive = isLive;
            Update(amendment, nameof(Model.DataModel.Amendment.IsLive));
        }

        public void SetIsArchived(bool isArchived, Model.DataModel.Amendment amendment)
        {
            amendment.IsArchived = isArchived;
            Update(amendment, nameof(Model.DataModel.Amendment.IsArchived));
        }
    }
}


/*
 * var contact = new Contact{Id = 1};
contact.FirstName = "Something new";
context.Entry(contact).Property("FirstName").IsModified = true;
context.SaveChanges();
 */
