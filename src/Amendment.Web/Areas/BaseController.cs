using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amendment.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Amendment.Web.Areas.Admin
{
    [Area("Admin")]
    [Authorize(Roles = "System Administrator")]
    public abstract class BaseController : Controller
    {
    }
}
