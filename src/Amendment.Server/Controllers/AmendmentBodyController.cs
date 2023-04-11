using Amendment.Server.Extensions;
using Amendment.Server.Mediator.Commands.AmendmentBodyCommands;
using Amendment.Server.Mediator.Commands.AmendmentCommands;
using Amendment.Server.Mediator.Queries.AmendmentBodyQueries;
using Amendment.Server.Mediator.Queries.AmendmentQueries;
using Amendment.Shared.Requests;
using Amendment.Shared.Responses;
using Amendment.Shared;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Amendment.Server.Controllers
{
    [Route("api/Amendment/{amendmentId}/Body")]
    [ApiController]
    [Authorize(Roles = RoleGroups.AdminTranslatorAmendEditor)]
    public class AmendmentBodyController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AmendmentBodyController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IResult> Get(int amendmentId)
        {
            var results = await _mediator.Send(new GetAllAmendmentBodiesQuery(amendmentId));
            return results.ToResult();
        }

        [HttpGet("{id}")]
        public async Task<IResult> Get(int amendmentId, int id)
        {
            var results = await _mediator.Send(new GetSingleAmendmentBodyQuery(id, amendmentId));
            return results.ToResult();
        }

        [HttpPost]
        public async Task<IResult> Post(int amendmentId, [FromBody] AmendmentBodyRequest model)
        {
            var command = model.Adapt<CreateAmendmentBodyCommand>();
            command.SavingUserId = SignedInUserId;
            command.AmendId = amendmentId;
            var results = await _mediator.Send(command);
            if (!results.IsSuccess)
                return results.ToResult();

            if (results is ApiSuccessResult<AmendmentBodyResponse> typedResults)
            {
                var url = Url.Action(nameof(Get), new { id = typedResults.Result?.Id, amendmentId }) ?? "/";
                return typedResults.ToResult(url);
            }
            return results.ToResult();
        }

        [HttpPut("{id}")]
        public async Task<IResult> Put(int amendmentId, int id, [FromBody] AmendmentBodyRequest model)
        {
            var command = model.Adapt<UpdateAmendmentBodyCommand>();
            command.SavingUserId = SignedInUserId;
            command.Id = id;
            command.AmendId = amendmentId;
            var results = await _mediator.Send(command);

            return results.ToResult();
        }

        [HttpDelete("{id}")]
        public async Task<IResult> Delete(int amendmentId, int id)
        {
            var command = new DeleteAmendmentBodyCommand(id, amendmentId, SignedInUserId);
            var results = await _mediator.Send(command);
            return results.ToResult();
        }
    }
}
