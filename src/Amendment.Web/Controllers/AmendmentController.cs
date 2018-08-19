using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amendment.Model.ViewModel.Amendment;
using Amendment.Model.ViewModel.User;
using Amendment.Service;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Amendment.Web.Controllers
{
    [Authorize]
    public class AmendmentController : Controller
    {
        private readonly IAmendmentService _amendmentService;
        private readonly IAmendmentBodyService _amendmentBodyService;
        private readonly IMapper _mapper;

        public AmendmentController(IAmendmentService amendmentService, IAmendmentBodyService amendmentBodyService, IMapper mapper)
        {
            _amendmentService = amendmentService;
            _amendmentBodyService = amendmentBodyService;
            _mapper = mapper;
        }

        public async Task<ActionResult> Index()
        {
            var amendments = await _amendmentService.GetAllAsync();
            var model = _mapper.Map<List<AmendmentDetailsViewModel>>(amendments);
            return View(model);
        }

        public async Task<ActionResult> Detail(int id)
        {
            return View();
        }

        [Authorize(Roles = "System Administrator, Amendment Editor")]
        public async Task<ActionResult> Create()
        {
            return View(new AmendmentCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "System Administrator, Amendment Editor")]
        public async Task<ActionResult> Create(AmendmentCreateViewModel model)
        {
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "System Administrator, Amendment Editor")]
        public async Task<ActionResult> Edit(int id)
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "System Administrator, Amendment Editor")]
        public async Task<ActionResult> Edit(int id, AmendmentEditViewModel model)
        {
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "System Administrator, Amendment Editor")]
        public async Task<ActionResult> Delete(int id)
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "System Administrator, Amendment Editor")]
        public async Task<ActionResult> Delete(int id, IFormCollection collection)
        {
            return RedirectToAction(nameof(Index));
        }
    }
}
