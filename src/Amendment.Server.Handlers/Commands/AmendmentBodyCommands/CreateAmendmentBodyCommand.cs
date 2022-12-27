using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amendment.Shared;
using Amendment.Shared.Enums;
using Amendment.Shared.Responses;
using MediatR;

namespace Amendment.Server.Mediator.Commands.AmendmentBodyCommands
{
    public sealed class CreateAmendmentBodyCommand : IRequest<IApiResult<AmendmentBodyResponse>>
    {
        public int SavingUserId { get; set; }
        public int AmendId { get; set; }
        public int LanguageId { get; set; }
        public string AmendBody { get; set; }
        public AmendmentBodyStatus AmendStatus { get; set; }
        public bool IsLive { get; set; }
    }
}
