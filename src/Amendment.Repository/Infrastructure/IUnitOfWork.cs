using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;

namespace Amendment.Repository.Infrastructure
{
    public interface IUnitOfWork
    {
        void SaveChanges(int userId);
        IDbContextTransaction BeginTransaction();
        void CommitTransaction();
        void RollbackTransaction();

        Task SaveChangesAsync(int userId);
        Task<IDbContextTransaction> BeginTransactionAsync();
        bool InMemory { get; }
    }
}
