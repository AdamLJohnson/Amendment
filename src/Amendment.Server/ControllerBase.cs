using Microsoft.AspNetCore.Authorization;

namespace Amendment.Server
{
    [Authorize]
    public class ControllerBase : Microsoft.AspNetCore.Mvc.ControllerBase
    {
    }
}
