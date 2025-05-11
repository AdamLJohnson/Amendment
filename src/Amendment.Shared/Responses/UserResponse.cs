using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amendment.Shared.Requests;

namespace Amendment.Shared.Responses
{
    public sealed class UserResponse
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Name { get; set; }
        public int[] Roles { get; set; } = Array.Empty<int>();
        public bool RequirePasswordChange { get; set; }

        public UserRequest ToRequest()
        {
            return new UserRequest
            {
                Username = Username,
                Email = Email,
                Name = Name,
                Roles = Roles,
                RequirePasswordChange = RequirePasswordChange
            };
        }
    }
}
