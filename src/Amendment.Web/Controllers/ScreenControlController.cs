using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amendment.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Amendment.Web.Controllers
{
    [Authorize(Roles = "Screen Controller, System Administrator")]
    public class ScreenControlController : Controller
    {
        private readonly IScreenControlService _screenControlService;
        private readonly IAmendmentService _amendmentService;

        public ScreenControlController(IScreenControlService screenControlService, IAmendmentService amendmentService)
        {
            _screenControlService = screenControlService;
            _amendmentService = amendmentService;
        }

        public async Task<ActionResult> Index()
        {
            var amendment = await _amendmentService.GetLiveAsync();

            return View(amendment ?? new Amendment.Model.DataModel.Amendment());
        }
    }
}
