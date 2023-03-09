using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amendment.Shared.Requests;

namespace Amendment.Shared.Validators
{
    public sealed class UserRequestValidator : AbstractValidator<UserRequest>
    {
        public UserRequestValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty();
            RuleFor(x => x.Name)
                .NotEmpty();
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();
            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password)
                .WithMessage("Passwords do not match");
            RuleFor(x => x.ConfirmPassword)
                .MaximumLength(30);
            RuleFor(x => x.Password)
                .MaximumLength(30);

            When(x => x.IsCreate, () =>
            {
                RuleFor(x => x.Password)
                    .NotEmpty();
                RuleFor(x => x.ConfirmPassword)
                    .NotEmpty();
            });
        }
    }
}
