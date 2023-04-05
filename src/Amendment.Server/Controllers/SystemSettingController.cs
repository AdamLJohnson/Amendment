using Amendment.Server.Extensions;
using Amendment.Server.Mediator.Commands.SystemSettingCommands;
using Amendment.Server.Mediator.Queries.SystemSettingQueries;
using Amendment.Shared;
using Amendment.Shared.Requests;
using Amendment.Shared.Responses;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Amendment.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SystemSettingController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SystemSettingController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IResult> Get()
        {
            var results = await _mediator.Send(new GetAllSystemSettingsQuery());
            return results.ToResult();
        }

        [HttpGet("{id}")]
        public async Task<IResult> Get(int id)
        {
            var results = await _mediator.Send(new GetSingleSystemSettingQuery(id));
            return results.ToResult();
        }

        [HttpPost]
        public async Task<IResult> Post([FromBody] SystemSettingRequest model)
        {
            var command = model.Adapt<CreateSystemSettingCommand>();
            command.SavingUserId = SignedInUserId;
            var results = await _mediator.Send(command);
            if (!results.IsSuccess)
                return results.ToResult();

            if (results is ApiSuccessResult<SystemSettingResponse> typedResults)
            {
                var url = Url.Action(nameof(Get), new { id = typedResults.Result?.Id }) ?? "/";
                return typedResults.ToResult(url);
            }
            return results.ToResult();
        }

        [HttpPut("{id}")]
        public async Task<IResult> Put(int id, [FromBody] SystemSettingRequest model)
        {
            var command = model.Adapt<UpdateSystemSettingCommand>();
            command.SavingUserId = SignedInUserId;
            command.Id = id;
            var results = await _mediator.Send(command);

            return results.ToResult();
        }

        [HttpDelete("{id}")]
        public async Task<IResult> Delete(int id)
        {
            var command = new DeleteSystemSettingCommand(id, SignedInUserId);
            var results = await _mediator.Send(command);
            return results.ToResult();
        }
    }
}
