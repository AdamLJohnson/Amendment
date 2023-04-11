using Amendment.Model.DataModel;
using Amendment.Server.Mediator.Commands.AmendmentBodyCommands;
using Amendment.Server.Mediator.Commands.AmendmentCommands;
using Amendment.Server.Mediator.Queries.AmendmentBodyQueries;
using Amendment.Server.Mediator.Queries.AmendmentQueries;
using Amendment.Shared;
using Amendment.Shared.SignalRCommands;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Amendment.Server.Hubs
{
    [Authorize]
    public class AmendmentHub : Hub
    {
        private readonly IMediator _mediator;

        public AmendmentHub(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize (Roles = RoleGroups.AdminScreenController)]
        public async Task AmendmentBodyLive(SetAmendmentBodyLiveCommands bodies)
        {
            await _mediator.Send(new BulkSetAmendmentBodyLiveCommand(bodies));
        }

        [Authorize(Roles = RoleGroups.AdminScreenController)]
        public async Task AmendmentBodyPage(SetAmendmentBodyPageCommands bodies)
        {
            await _mediator.Send(new BulkSetAmendmentBodyPageCommand(bodies));
        }

        protected int SignedInUserId
        {
            get
            {
                if (int.TryParse(Context.User?.Claims.FirstOrDefault(c => c?.Type == "id")?.Value, out int result))
                    return result;

                return 0;
            }
        }
    }
}
