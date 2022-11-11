using Amendment.Shared.Responses;
using MediatR;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amendment.Shared.Requests
{
    public sealed  class AccountLoginRequest : IRequest<AccountLoginResponse>
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}
