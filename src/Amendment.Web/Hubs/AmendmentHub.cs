using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Amendment.Web.Hubs
{
    [Authorize]
    public class AmendmentHub : Hub
    {
    }
}
