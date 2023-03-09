using Amendment.Server.Extensions;
using Amendment.Server.Mediator.Queries.RoleQueries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Amendment.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "System Administrator")]
public class RoleController : ControllerBase
{
    private readonly IMediator _mediator;

    public RoleController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IResult> Get()
    {
        var results = await _mediator.Send(new GetAllRolesQuery());
        return results.ToResult();
    }
}
