using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amendment.Shared.Responses
{
    public sealed class AccountLoginResponse
    {
        public bool IsAuthSuccessful { get; set; }
        public string? Token { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
