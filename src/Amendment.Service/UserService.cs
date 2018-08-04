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
        void CreateAsync(User user);
        void UpdateAsync(User user);
        void DeleteAsync(User user);
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

        public void CreateAsync(User user)
        {
            throw new NotImplementedException();
        }

        public void UpdateAsync(User user)
        {
            throw new NotImplementedException();
        }

        public void DeleteAsync(User user)
        {
            throw new NotImplementedException();
        }

        public Task<User> GetAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<User> GetAsync(string userName)
        {
            throw new NotImplementedException();
        }

        public Task<List<User>> GetAllAsync()
        {
            throw new NotImplementedException();
        }
    }
}
