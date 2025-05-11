using System.Security.Claims;

namespace Amendment.Server.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static int GetUserId(this ClaimsPrincipal principal)
        {
            if (principal == null)
                return 0;

            var claim = principal.Claims.FirstOrDefault(c => c.Type == "id");
            if (claim != null && int.TryParse(claim.Value, out int userId))
                return userId;

            return 0;
        }
    }
}
