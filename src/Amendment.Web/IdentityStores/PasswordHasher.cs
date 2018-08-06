using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amendment.Service;
using Microsoft.AspNetCore.Identity;

namespace Amendment.Web.IdentityStores
{
    public class PasswordHasher<TUser> : IPasswordHasher<TUser> where TUser : class
    {
        private readonly IPasswordHashService _passwordHashService;

        public PasswordHasher(IPasswordHashService passwordHashService)
        {
            _passwordHashService = passwordHashService;
        }

        public string HashPassword(TUser user, string password)
        {
            return _passwordHashService.HashPassword(password);
        }

        public PasswordVerificationResult VerifyHashedPassword(TUser user, string hashedPassword, string providedPassword)
        {
            return (_passwordHashService.VerifyHashedPassword(hashedPassword, providedPassword)
                ? PasswordVerificationResult.Success
                : PasswordVerificationResult.Failed);
        }
    }
}
