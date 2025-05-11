using Amendment.Shared.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amendment.Shared.Requests
{
    public sealed class UserRequest
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Name { get; set; }
        public string? Password { get; set; }
        public string? ConfirmPassword { get; set; }
        public int[] Roles { get; set; } = Array.Empty<int>();
        public bool IsCreate { get; set; } = false;
        public bool RequirePasswordChange { get; set; } = true;
    }
}
