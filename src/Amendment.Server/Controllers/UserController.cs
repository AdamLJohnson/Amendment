using Amendment.Server.Extensions;
using Amendment.Server.Mediator.Commands.UserCommands;
using Amendment.Server.Mediator.Queries.UserQueries;
using Amendment.Shared;
using Amendment.Shared.Requests;
using Amendment.Shared.Responses;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Amendment.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "System Administrator")]
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IResult> Get()
    {
        var results = await _mediator.Send(new GetAllUsersQuery());
        return results.ToResult();
    }

    [HttpGet("{id}")]
    public async Task<IResult> Get(int id)
    {
        var results = await _mediator.Send(new GetSingleUserQuery(id));
        return results.ToResult();
    }

    [HttpPost]
    public async Task<IResult> Post([FromBody] UserRequest model)
    {
        var command = model.Adapt<CreateUserCommand>();
        command.SavingUserId = SignedInUserId;
        var results = await _mediator.Send(command);
        if (!results.IsSuccess)
            return results.ToResult();

        if (results is ApiSuccessResult<UserResponse>)
        {
            var typedResults = (ApiSuccessResult<UserResponse>)results;
            return results.ToResult(Url.Action(nameof(Get), new { id = typedResults.Result.Id }));
        }   
        return results.ToResult();
    }

    [HttpPut("{id}")]
    public async Task<IResult> Put(int id, [FromBody] UserRequest model)
    {
        var command = model.Adapt<UpdateUserCommand>();
        command.SavingUserId = SignedInUserId;
        command.Id = id;
        var results = await _mediator.Send(command);

        return results.ToResult();
    }

    [HttpDelete("{id}")]
    public async Task<IResult> Delete(int id)
    {
        var command = new DeleteUserCommand(id, SignedInUserId);
        var results = await _mediator.Send(command);
        return results.ToResult();
    }
}
