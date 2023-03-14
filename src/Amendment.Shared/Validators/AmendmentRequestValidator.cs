using Amendment.Shared.Requests;
using FluentValidation;

namespace Amendment.Shared.Validators;

public sealed class AmendmentRequestValidator : AbstractValidator<AmendmentRequest>
{
    public AmendmentRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEqual("not allowed")
            .NotEmpty();
        RuleFor(x => x.Author)
            .NotEmpty();
        RuleFor(x => x.PrimaryLanguageId)
            .GreaterThan(0)
            .WithMessage("Please select a language");
    }
}