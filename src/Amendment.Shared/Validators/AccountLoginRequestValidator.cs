using Amendment.Shared.Requests;
using FluentValidation;

namespace Amendment.Shared.Validators
{
    public sealed class AccountLoginRequestValidator : AbstractValidator<AccountLoginRequest>
    {
        public AccountLoginRequestValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty();

            RuleFor(x => x.Password)
                .NotEmpty().NotEqual("not allowed");
        }
    }
}
