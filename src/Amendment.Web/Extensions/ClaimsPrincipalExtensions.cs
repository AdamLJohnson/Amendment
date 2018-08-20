using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace System
{
    public static class ClaimsPrincipalExtensions
    {
        public static int UserId(this ClaimsPrincipal user)
        {
            return user.Claims.Where(c => c.Type == ClaimTypes.NameIdentifier).Select(c => Convert.ToInt32(c.Value))
                .FirstOrDefault();
        }
    }
}
