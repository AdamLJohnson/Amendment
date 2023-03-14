using Amendment.Shared;
using Amendment.Shared.Requests;
using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amendment.Server.Mediator.Commands
{
    public sealed class AccountLoginCommand : IRequest<IApiResult>
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
    }

    public sealed class AccountLoginCommandValidator : AbstractValidator<AccountLoginCommand>
    {
        public AccountLoginCommandValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty();

            RuleFor(x => x.Password)
                .NotEmpty().NotEqual("not allowed");
        }
    }
}
