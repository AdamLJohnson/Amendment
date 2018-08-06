using System;
using System.Collections.Generic;
using System.Text;

namespace Amendment.Service
{
    public interface IPasswordHashService
    {
        string HashPassword(string password);
        bool VerifyHashedPassword(string hashedPassword, string providedPassword);
    }

    public class PasswordHashService : IPasswordHashService
    {
        private int _saltLength = 12;
        public PasswordHashService() { }

        public PasswordHashService(int saltLength)
        {
            _saltLength = saltLength;
        }

        public string HashPassword(string password)
        {
            var salt = BCrypt.Net.BCrypt.GenerateSalt(_saltLength);
            var output = BCrypt.Net.BCrypt.HashPassword(password, salt);
            return output;
        }

        public bool VerifyHashedPassword(string hashedPassword, string providedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword);
        }
    }
}
