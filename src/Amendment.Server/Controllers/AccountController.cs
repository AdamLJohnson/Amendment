using Amendment.Model.DataModel;
using Amendment.Server.Extensions;
using Amendment.Server.Mediator.Commands;
using Amendment.Shared.Requests;
using Amendment.Shared.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Mapster;

namespace Amendment.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AccountController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("Login")]
        [AllowAnonymous]
        public async Task<IResult> Login([FromBody] AccountLoginRequest userForAuthentication)
        {
            var command = userForAuthentication.Adapt<AccountLoginCommand>();
            var result = await _mediator.Send(command);
            return result.ToResult();
        }

        [HttpPost("RefreshToken")]
        [AllowAnonymous]
        public async Task<IResult> RefreshToken([FromBody] RefreshTokenRequest model)
        {
            var command = model.Adapt<RefreshTokenCommand>();
            var result = await _mediator.Send(command);
            return result.ToResult();
        }

    }
}