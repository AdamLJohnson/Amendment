using Amendment.Shared;
using Amendment.Shared.Requests;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amendment.Shared.Responses;

namespace Amendment.Server.Mediator.Commands.UserCommands
{
    public sealed class CreateUserCommand : IRequest<IApiResult>
    {
        public int SavingUserId { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Name { get; set; }
        public int[] Roles { get; set; } = Array.Empty<int>();
        public string? Password { get; set; }
        public bool RequirePasswordChange { get; set; }
    }
}
