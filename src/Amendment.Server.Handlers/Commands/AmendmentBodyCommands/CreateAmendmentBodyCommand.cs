using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amendment.Shared;
using Amendment.Shared.Enums;
using Amendment.Shared.Requests;
using Amendment.Shared.Responses;
using FluentValidation;
using MediatR;

namespace Amendment.Server.Mediator.Commands.AmendmentBodyCommands
{
    public sealed class CreateAmendmentBodyCommand : IRequest<IApiResult>
    {
        public int SavingUserId { get; set; }
        public int AmendId { get; set; }
        public int LanguageId { get; set; }
        public string? AmendBody { get; set; }
        public AmendmentBodyStatus AmendStatus { get; set; }
        public bool IsLive { get; set; }
    }

    public sealed class CreateAmendmentBodyCommandValidator : AbstractValidator<CreateAmendmentBodyCommand>
    {
        public CreateAmendmentBodyCommandValidator()
        {
            RuleFor(x => x.LanguageId)
                .NotEqual(0).WithMessage("Language is required")
                //.NotEmpty().WithMessage("Language is required")
                ;
            RuleFor(x => x.AmendBody)
                .NotEmpty();
        }
    }
}
