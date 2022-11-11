using Amendment.Shared.Requests;
using Amendment.Shared.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<Results<Ok<AccountLoginResponse>, UnauthorizedHttpResult>> Login([FromBody] AccountLoginRequest userForAuthentication)
        {
            var result = await _mediator.Send(userForAuthentication);
            return !result.IsAuthSuccessful ? TypedResults.Unauthorized() : TypedResults.Ok(result);
        }
    }
}