using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amendment.Model.DataModel;
using Amendment.Service;
using Microsoft.AspNetCore.Identity;

namespace Amendment.Web.IdentityStores
{
    public class UserStore : IUserStore<User>, IUserRoleStore<User>, IUserPasswordStore<User>, IUserSecurityStampStore<User>
    {
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;

        public UserStore(IUserService userService, IRoleService roleService)
        {
            _userService = userService;
            _roleService = roleService;
        }

        public void Dispose()
        {
            
        }

        public Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return Task.FromResult(user.Id.ToString());
        }

        public Task<string> GetUserNameAsync(User user, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return Task.FromResult(user.Username);
        }

        public Task SetUserNameAsync(User user, string userName, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            user.Username = userName;
            return Task.FromResult(0);
        }

        public Task<string> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return Task.FromResult(user.Username);
        }

        public Task SetNormalizedUserNameAsync(User user, string normalizedName, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            user.Username = normalizedName;
            return Task.FromResult(0);
        }

        public async Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken)
        {
            try
            {
                if (user == null)
                    throw new ArgumentNullException(nameof(user));

                await _userService.CreateAsync(user, -998);
                return IdentityResult.Success;
            }
            catch (Exception ex)
            {
                return IdentityResult.Failed(new IdentityError() { Description = ex.GetBaseException().Message });
            }
        }

        public async Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken)
        {
            try
            {
                if (user == null)
                    throw new ArgumentNullException(nameof(user));

                await _userService.UpdateAsync(user, -998);
                return IdentityResult.Success;
            }
            catch (Exception ex)
            {
                return IdentityResult.Failed(new IdentityError() { Description = ex.GetBaseException().Message });
            }
        }

        public async Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken)
        {
            try
            {
                if (user == null)
                    throw new ArgumentNullException(nameof(user));

                await _userService.DeleteAsync(user, -998);
                return IdentityResult.Success;
            }
            catch (Exception ex)
            {
                return IdentityResult.Failed(new IdentityError() { Description = ex.GetBaseException().Message });
            }
        }

        public Task<User> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            if (userId == null) throw new ArgumentNullException(nameof(userId));
            return _userService.GetAsync(Convert.ToInt32(userId));
        }

        public async Task<User> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            if (normalizedUserName == null) throw new ArgumentNullException(nameof(normalizedUserName));
            var output = await _userService.GetAsync(normalizedUserName);
            return output;
        }

        public async Task AddToRoleAsync(User user, string roleName, CancellationToken cancellationToken)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (roleName == null) throw new ArgumentNullException(nameof(roleName));

            var role = await _roleService.GetByNameAsync(roleName);
            if (role == null) throw new Exception(roleName + " Role Not Found");

            user.UserXRoles.Add(new UserXRole() { RoleId = role.Id, UserId = user.Id, User = user, Role = role });
        }

        public Task RemoveFromRoleAsync(User user, string roleName, CancellationToken cancellationToken)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (roleName == null) throw new ArgumentNullException(nameof(roleName));

            var role = user.UserXRoles.FirstOrDefault(ur => ur.Role.Name == roleName);
            if (role != null)
            {
                var userRole = user.UserXRoles.SingleOrDefault(ur => ur.RoleId == role.RoleId && ur.UserId == user.Id);
                if (userRole != null)
                {
                    user.UserXRoles.Remove(userRole);
                }
            }

            return Task.FromResult(0);
        }

        public Task<IList<string>> GetRolesAsync(User user, CancellationToken cancellationToken)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            IList<string> output = user.UserXRoles.Select(ur => ur.Role.Name).ToList();
            return Task.FromResult(output);
        }

        public Task<bool> IsInRoleAsync(User user, string roleName, CancellationToken cancellationToken)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (roleName == null) throw new ArgumentNullException(nameof(roleName));
            return Task.FromResult(user.UserXRoles.Any(ur => ur.Role.Name == roleName));
        }

        public Task<IList<User>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
        {
            if (roleName == null) throw new ArgumentNullException(nameof(roleName));
            throw new NotImplementedException();
        }

        public Task SetPasswordHashAsync(User user, string passwordHash, CancellationToken cancellationToken)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            user.Password = passwordHash;
            return Task.FromResult(0);
        }

        public Task<string> GetPasswordHashAsync(User user, CancellationToken cancellationToken)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            return Task.FromResult(user.Password);
        }

        public Task<bool> HasPasswordAsync(User user, CancellationToken cancellationToken)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            return Task.FromResult(string.IsNullOrEmpty(user.Password));
        }

        public Task SetSecurityStampAsync(User user, string stamp, CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }

        public Task<string> GetSecurityStampAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult("fake security stamp");
        }
    }
}
