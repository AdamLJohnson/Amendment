﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Amendment.Server.Hubs
{
    [AllowAnonymous]
    public class ScreenHub : Hub
    {
    }
}
