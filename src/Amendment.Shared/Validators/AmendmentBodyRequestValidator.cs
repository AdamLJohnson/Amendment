using Amendment.Shared.Requests;
using FluentValidation;

namespace Amendment.Shared.Validators;

public sealed class AmendmentBodyRequestValidator : AbstractValidator<AmendmentBodyRequest>
{
    public AmendmentBodyRequestValidator()
    {
        RuleFor(x => x.LanguageId)
            .NotEmpty();
    }
}