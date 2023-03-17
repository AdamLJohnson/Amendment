using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amendment.Model.DataModel;
using Amendment.Repository.Infrastructure;
using Amendment.Service.Infrastructure;

namespace Amendment.Service
{
    public interface IUserService : IDataService<User>
    {
        Task<User> GetAsync(string userName);
        Task<User> GetForToastAsync(int userId);
    }

    public class UserService : BaseDataService<User>, IUserService
    {
        private readonly IRepository<User> _repository;
        private readonly IUnitOfWork _unitOfWork;

        public UserService(IRepository<User> repository, IUnitOfWork unitOfWork) : base(repository, unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public Task<User> GetAsync(string userName)
        {
            return _repository.GetAsync(u => u.Username.ToLower() == userName.ToLower());
        }

        public override async Task<IOperationResult> CreateAsync(User item, int userId)
        {
            var dupeCount = await _repository.CountAsync(u => u.Username == item.Username);
            if (dupeCount > 0)
                return new OperationResult(OperationType.Create, false, $"A user is already present with the username '{item.Username}'");
            return await base.CreateAsync(item, userId);
        }

        public override async Task<IOperationResult> UpdateAsync(User item, int userId)
        {
            var dupeCount = await _repository.CountAsync(u => u.Username == item.Username && u.Id != item.Id);
            if (dupeCount > 0)
                return new OperationResult(OperationType.Update, false, $"A user is already present with the username '{item.Username}'");
            return await base.UpdateAsync(item, userId);
        }

        public async Task<User> GetForToastAsync(int userId)
        {
            var user = await _repository.GetByIdAsync(userId);
            if (user == null)
                return null;

            return new User
            {
                Id = user.Id,
                Name = user.Name,
                Username = user.Username
            };
        }
    }
}
