using Amendment.Model.DataModel;
using Amendment.Server.Mediator.Commands.AmendmentBodyCommands;
using Amendment.Server.Mediator.Commands.AmendmentCommands;
using Amendment.Server.Mediator.Queries.AmendmentBodyQueries;
using Amendment.Server.Mediator.Queries.AmendmentQueries;
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

        public async Task  AmendmentLive(int amendmentId, bool isLive)
        {
            var amendment = await _mediator.Send(new GetSingleAmendmentQuery(amendmentId));
            var command = amendment.Result.Adapt<UpdateAmendmentCommand>();
            command.SavingUserId = amendmentId;
            command.IsLive = isLive;
            await _mediator.Send(command);
        }

        public async Task AmendmentBodyLive(SetAmendmentBodyLiveCommands bodies)
        {
            foreach (var bodyInfo in bodies.Commands)
            {
                var body = await _mediator.Send(new GetSingleAmendmentBodyQuery(bodyInfo.Id, bodyInfo.AmendId));
                var command = body.Result.Adapt<UpdateAmendmentBodyCommand>();
                command.SavingUserId = SignedInUserId;
                command.IsLive = bodyInfo.IsLive;
                await _mediator.Send(command);
            }
        }

        public async Task AmendmentBodyPage(SetAmendmentBodyPageCommands bodies)
        {
            foreach (var bodyInfo in bodies.Commands)
            {
                var body = await _mediator.Send(new GetSingleAmendmentBodyQuery(bodyInfo.Id, bodyInfo.AmendId));
                var command = body.Result.Adapt<UpdateAmendmentBodyCommand>();
                command.SavingUserId = SignedInUserId;
                command.Page = bodyInfo.Page;
               await _mediator.Send(command);
            }
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
