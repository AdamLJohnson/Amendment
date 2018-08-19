using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amendment.Model.DataModel;
using Amendment.Model.ViewModel.Amendment;
using Amendment.Model.ViewModel.User;
using Amendment.Service;
using Amendment.Service.Infrastructure;
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
        private readonly IReadOnlyDataService<Language> _languageDataService;

        public AmendmentController(IAmendmentService amendmentService, IAmendmentBodyService amendmentBodyService, IMapper mapper, IReadOnlyDataService<Language> languageDataService)
        {
            _amendmentService = amendmentService;
            _amendmentBodyService = amendmentBodyService;
            _mapper = mapper;
            _languageDataService = languageDataService;
        }

        public async Task<ActionResult> Index()
        {
            var amendments = await _amendmentService.GetAllAsync();
            var model = _mapper.Map<List<AmendmentDetailsViewModel>>(amendments);
            return View(model);
        }

        public async Task<ActionResult> Detail(int id)
        {
            return View(new AmendmentDetailsViewModel());
        }

        [Authorize(Roles = "System Administrator, Amendment Editor")]
        public async Task<ActionResult> Create()
        {
            var model = new AmendmentCreateViewModel();
            model.Languages = await _languageDataService.GetAllAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "System Administrator, Amendment Editor")]
        public async Task<ActionResult> Create(AmendmentCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Languages = await _languageDataService.GetAllAsync();
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "System Administrator, Amendment Editor")]
        public async Task<ActionResult> Edit(int id)
        {
            var amendment = await _amendmentService.GetAsync(id);
            if (amendment == null)
                return NotFound();
            
            var model = _mapper.Map<AmendmentEditViewModel>(amendment);
            model.Languages = await _languageDataService.GetAllAsync();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "System Administrator, Amendment Editor")]
        public async Task<ActionResult> Edit(int id, AmendmentEditViewModel model)
        {
            var amendment = await _amendmentService.GetAsync(id);
            if (amendment == null)
                return NotFound();

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "System Administrator, Amendment Editor")]
        public async Task<ActionResult> Delete(int id)
        {
            return View(new AmendmentDetailsViewModel());
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
