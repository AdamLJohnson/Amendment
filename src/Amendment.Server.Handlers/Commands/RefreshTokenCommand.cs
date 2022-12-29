using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amendment.Shared;
using Amendment.Shared.Responses;
using MediatR;

namespace Amendment.Server.Mediator.Commands
{
    public class RefreshTokenCommand : IRequest<IApiResult<AccountLoginResponse>>
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
    }
}
