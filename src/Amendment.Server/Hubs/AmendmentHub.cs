﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Amendment.Server.Hubs
{
    [Authorize]
    public class AmendmentHub : Hub
    {
    }
}