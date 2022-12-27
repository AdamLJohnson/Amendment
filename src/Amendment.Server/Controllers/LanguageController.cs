using Amendment.Server.Extensions;
using Amendment.Server.Mediator.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Amendment.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LanguageController : ControllerBase
    {
        private readonly IMediator _mediator;

        public LanguageController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [AllowAnonymous]
        [HttpGet]
        [OutputCache(Duration = 300)]
        public async Task<IResult> Get()
        {
            var results = await _mediator.Send(new GetAllLanguagesQuery());
            return results.ToResult();
        }
    }
}
