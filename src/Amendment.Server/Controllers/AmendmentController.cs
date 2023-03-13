using Amendment.Server.Extensions;
using Amendment.Server.Mediator.Commands.AmendmentCommands;
using Amendment.Server.Mediator.Queries.AmendmentQueries;
using Amendment.Shared.Requests;
using Amendment.Shared.Responses;
using Amendment.Shared;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Amendment.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AmendmentController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AmendmentController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IResult> Get()
        {
            var results = await _mediator.Send(new GetAllAmendmentsQuery());
            return results.ToResult();
        }

        [HttpGet("Live")]
        public async Task<IResult> GetLive()
        {
            var results = await _mediator.Send(new GetLiveAmendmentQuery());
            return results.ToResult();
        }

        [HttpGet("{id}")]
        public async Task<IResult> Get(int id)
        {
            var results = await _mediator.Send(new GetSingleAmendmentQuery(id));
            return results.ToResult();
        }

        [HttpPost]
        public async Task<IResult> Post([FromBody] AmendmentRequest model)
        {
            var command = model.Adapt<CreateAmendmentCommand>();
            command.SavingUserId = SignedInUserId;
            var results = await _mediator.Send(command);
            if (!results.IsSuccess)
                return results.ToResult();

            if (results is ApiSuccessResult<AmendmentResponse>)
            {
                var typedResults = (ApiSuccessResult<AmendmentResponse>)results;
                return results.ToResult(Url.Action(nameof(Get), new { id = typedResults.Result.Id }));
            }
            return results.ToResult();
        }

        [HttpPut("{id}")]
        public async Task<IResult> Put(int id, [FromBody] AmendmentRequest model)
        {
            var command = model.Adapt<UpdateAmendmentCommand>();
            command.SavingUserId = SignedInUserId;
            command.Id = id;
            var results = await _mediator.Send(command);

            return results.ToResult();
        }

        [HttpDelete("{id}")]
        public async Task<IResult> Delete(int id)
        {
            var command = new DeleteAmendmentCommand(id, SignedInUserId);
            var results = await _mediator.Send(command);
            return results.ToResult();
        }
    }
}
