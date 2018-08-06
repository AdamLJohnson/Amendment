using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amendment.Model.DataModel;
using Amendment.Model.Selectors;
using Amendment.Repository.Infrastructure;

namespace Amendment.Service
{
    public interface IUserService
    {
        Task CreateAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(User user);
        Task<User> GetAsync(int id);
        Task<User> GetAsync(string userName);
        Task<List<User>> GetAllAsync();
    }

    public class UserService : IUserService
    {
        private readonly IRepository<User> _repository;
        private readonly IUnitOfWork _unitOfWork;

        public UserService(IRepository<User> repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public Task CreateAsync(User user)
        {
            _repository.Add(user);
            return _unitOfWork.SaveChangesAsync();
        }

        public Task UpdateAsync(User user)
        {
            _repository.Update(user);
            return _unitOfWork.SaveChangesAsync();
        }

        public Task DeleteAsync(User user)
        {
            _repository.Delete(user);
            return _unitOfWork.SaveChangesAsync();
        }

        public Task<User> GetAsync(int id)
        {
            return _repository.GetByIdAsync(id);
        }

        public Task<User> GetAsync(string userName)
        {
            return _repository.GetAsync(u => String.Equals(u.Username, userName, StringComparison.CurrentCultureIgnoreCase));
        }

        public async Task<List<User>> GetAllAsync()
        {
            return (await _repository.GetAllAsync()).ToList();
        }
    }
}
