using System;
using System.Collections.Generic;
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
        private readonly IRepository<User, UserSelector> _repository;

        public UserService(IRepository<User, UserSelector> repository)
        {
            _repository = repository;
        }

        public Task CreateAsync(User user)
        {
            return _repository.InsertAsync(user);
        }

        public Task UpdateAsync(User user)
        {
            return _repository.UpdateAsync(user);
        }

        public Task DeleteAsync(User user)
        {
            return _repository.DeleteAsync(user.UserId);
        }

        public Task<User> GetAsync(int id)
        {
            return _repository.SelectSingleAsync(id);
        }

        public Task<User> GetAsync(string userName)
        {
            var selector = new UserSelector{ Username = userName };
            return _repository.SelectSingleAsync(selector);
        }

        public Task<List<User>> GetAllAsync()
        {
            return _repository.SelectAllAsync();
        }
    }
}
