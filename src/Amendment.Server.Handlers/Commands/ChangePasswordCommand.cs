using Amendment.Shared;
using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amendment.Server.Mediator.Commands
{
    public sealed class ChangePasswordCommand : IRequest<IApiResult>
    {
        public int UserId { get; set; }
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
        public bool IsFirstTimeLogin { get; set; }
    }

    public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordCommandValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0);

            // Current password is required unless it's a first-time login
            When(x => !x.IsFirstTimeLogin, () =>
            {
                RuleFor(x => x.CurrentPassword)
                    .NotEmpty();

                // New password must be different from current password
                RuleFor(x => x.NewPassword)
                    .NotEqual(x => x.CurrentPassword)
                    .WithMessage("New password must be different from current password");
            });

            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .MinimumLength(8)
                .Matches("[A-Z]")
                .Matches("[a-z]")
                .Matches("[0-9]")
                .Matches("[^a-zA-Z0-9]");
        }
    }
}
