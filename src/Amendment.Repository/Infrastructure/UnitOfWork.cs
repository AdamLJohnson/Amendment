using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Amendment.Repository.Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IDbFactory dbFactory;
        private AmendmentContext dbContext;
        private ILogger _logger;

        public UnitOfWork(IDbFactory dbFactory, ILoggerFactory loggerFactory)
        {
            this.dbFactory = dbFactory;
            _logger = loggerFactory.CreateLogger(nameof(UnitOfWork));
        }

        public AmendmentContext DbContext => dbContext ?? (dbContext = dbFactory.Init());

        public bool InMemory => !DbContext.HasConnection;

        public void SaveChanges()
        {
            try
            {
                //UpdateMetadataFields();
                DbContext.SaveChanges();
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                throw;
            }
        }

        public IDbContextTransaction BeginTransaction()
        {
            return DbContext.Database.BeginTransaction();
        }

        public void CommitTransaction()
        {
            DbContext.Database.CommitTransaction();
        }

        public void RollbackTransaction()
        {
            DbContext.Database.RollbackTransaction();
        }

        public async Task SaveChangesAsync()
        {
            try
            {
                //UpdateMetadataFields();
                await DbContext.SaveChangesAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                throw;
            }
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await DbContext.Database.BeginTransactionAsync();
        }

        //private void UpdateMetadataFields(int userId)
        //{
        //    //Todo: Code Update metadata fields
        //    foreach (var ent in DbContext.ChangeTracker.Entries().Where(p => p.State == EntityState.Added || p.State == EntityState.Deleted || p.State == EntityState.Modified))
        //    {
        //        if (ent.Entity != null && ent.Entity is ITableBase)
        //        {
        //            ITableBase entity = (ITableBase)ent.Entity;
        //            if (ent.State == EntityState.Added)
        //            {
        //                entity.EntryBy = userId;
        //                entity.EntryDate = DateTime.UtcNow;
        //            }
        //            entity.ModifiedBy = userId;
        //            entity.ModifiedDate = DateTime.UtcNow;
        //        }
        //    }
        //}
    }
}
