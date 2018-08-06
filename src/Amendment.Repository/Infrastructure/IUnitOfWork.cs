using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;

namespace Amendment.Repository.Infrastructure
{
    public interface IUnitOfWork
    {
        void SaveChanges();
        IDbContextTransaction BeginTransaction();
        void CommitTransaction();
        void RollbackTransaction();

        Task SaveChangesAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
        bool InMemory { get; }
    }
}
