using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amendment.Shared;
using Amendment.Shared.Responses;
using MediatR;

namespace Amendment.Server.Mediator.Commands.AmendmentCommands
{
    public sealed class CreateAmendmentCommand : IRequest<IApiResult<AmendmentResponse>>
    {
        public int SavingUserId { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Motion { get; set; }
        public string Source { get; set; }
        public string LegisId { get; set; }
        public int PrimaryLanguageId { get; set; }
    }
}
