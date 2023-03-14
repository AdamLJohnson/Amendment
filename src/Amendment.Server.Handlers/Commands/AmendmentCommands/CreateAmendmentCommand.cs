using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amendment.Shared;
using Amendment.Shared.Requests;
using Amendment.Shared.Responses;
using FluentValidation;
using MediatR;

namespace Amendment.Server.Mediator.Commands.AmendmentCommands
{
    public sealed class CreateAmendmentCommand : IRequest<IApiResult>
    {
        public int SavingUserId { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Motion { get; set; }
        public string Source { get; set; }
        public string LegisId { get; set; }
        public int PrimaryLanguageId { get; set; }
    }

    public sealed class CreateAmendmentCommandValidator : AbstractValidator<CreateAmendmentCommand>
    {
        public CreateAmendmentCommandValidator()
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
}
