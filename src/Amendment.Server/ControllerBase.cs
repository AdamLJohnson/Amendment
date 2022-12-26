using Microsoft.AspNetCore.Authorization;

namespace Amendment.Server
{
    [Authorize]
    public class ControllerBase : Microsoft.AspNetCore.Mvc.ControllerBase
    {
        protected int SignedInUserId
        {
            get
            {
                if (int.TryParse(User.Claims.FirstOrDefault(c => c?.Type == "id")?.Value, out int result))
                    return result;

                return 0;
            }
        }
    }
}
