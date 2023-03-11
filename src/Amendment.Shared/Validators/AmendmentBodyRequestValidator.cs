using Amendment.Shared.Requests;
using FluentValidation;

namespace Amendment.Shared.Validators;

public sealed class AmendmentBodyRequestValidator : AbstractValidator<AmendmentBodyRequest>
{
    public AmendmentBodyRequestValidator()
    {
        RuleFor(x => x.LanguageId)
            .NotEqual(0).WithMessage("Language is required")
            //.NotEmpty().WithMessage("Language is required")
            ;
        RuleFor(x => x.AmendBody)
            .NotEmpty();
    }
}