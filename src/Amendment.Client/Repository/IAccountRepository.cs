using Amendment.Shared.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amendment.Client.Repository
{
    public interface IAccountRepository
    {
        Task<bool> ChangePassword(ChangePasswordRequest request);
        Task<bool> GetRequirePasswordChange();
    }
}
