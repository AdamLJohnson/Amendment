using Amendment.Model.DataModel;
using Amendment.Repository;
using Microsoft.AspNetCore.Mvc;

namespace Amendment.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly ILogger<AccountController> _logger;
        private readonly AmendmentContext _context;

        public AccountController(ILogger<AccountController> logger, AmendmentContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        public IEnumerable<User> Get()
        {
            return _context.Set<User>().ToList();
        }
    }
}