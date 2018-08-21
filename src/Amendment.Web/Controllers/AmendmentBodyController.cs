using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amendment.Model.DataModel;
using Amendment.Model.ViewModel.AmendmentBody;
using Amendment.Service;
using Amendment.Service.Infrastructure;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Amendment.Web.Controllers
{
    [Authorize]
    public class AmendmentBodyController : Controller
    {
        private readonly IAmendmentBodyService _amendmentBodyService;
        private readonly IMapper _mapper;
        private readonly IReadOnlyDataService<Language> _languageDataService;

        public AmendmentBodyController(IAmendmentBodyService amendmentBodyService, IMapper mapper, IReadOnlyDataService<Language> languageDataService)
        {
            _amendmentBodyService = amendmentBodyService;
            _mapper = mapper;
            _languageDataService = languageDataService;
        }

        public async Task<ActionResult> Index(int amendmentId)
        {
            var bodies = await _amendmentBodyService.GetByAmentmentId(amendmentId);
            var model = _mapper.Map<List<AmendmentBodyDetailsViewModel>>(bodies);
            return View(model);
        }

        public async Task<ActionResult> Detail(int amendmentId, int id)
        {
            return View(new AmendmentBodyDetailsViewModel());
        }

        [Authorize(Roles = "System Administrator, Amendment Editor, Translator")]
        public async Task<ActionResult> Create(int amendmentId)
        {
            var bodies = await _amendmentBodyService.GetByAmentmentId(amendmentId);
            var alreadyCreatedLanguageIds = bodies.Select(b => b.LanguageId).ToList();

            var model = new AmendmentBodyCreateViewModel() { AmendId = amendmentId };

            model.Languages = (await _languageDataService.GetAllAsync()).Where(l => alreadyCreatedLanguageIds.All(c => c != l.Id)).ToList();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "System Administrator, Amendment Editor, Translator")]
        public async Task<ActionResult> Create(AmendmentBodyCreateViewModel model, int amendmentId)
        {
            if (!ModelState.IsValid)
            {
                var bodies = await _amendmentBodyService.GetByAmentmentId(amendmentId);
                var alreadyCreatedLanguageIds = bodies.Select(b => b.LanguageId).ToList();
                model.Languages = (await _languageDataService.GetAllAsync()).Where(l => alreadyCreatedLanguageIds.All(c => c != l.Id)).ToList();

                return View(model);
            }

            var body = _mapper.Map<AmendmentBody>(model);
            await _amendmentBodyService.CreateAsync(body, User.UserId());

            return RedirectToAction(nameof(Index), "Amendment");
        }

        [Authorize(Roles = "System Administrator, Amendment Editor, Translator")]
        public async Task<ActionResult> Edit(int amendmentId, int id)
        {
            var bodies = await _amendmentBodyService.GetByAmentmentId(amendmentId);
            var body = bodies.FirstOrDefault(b => b.Id == id);
            if (body == null)
                return NotFound();

            var model = _mapper.Map<AmendmentBodyEditViewModel>(body);
            
            var alreadyCreatedLanguageIds = bodies.Where(b => b.Id != id).Select(b => b.LanguageId).ToList();
            model.Languages = (await _languageDataService.GetAllAsync()).Where(l => alreadyCreatedLanguageIds.All(c => c != l.Id)).ToList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "System Administrator, Amendment Editor, Translator")]
        public async Task<ActionResult> Edit(int amendmentId, int id, AmendmentBodyEditViewModel model)
        {
            var bodies = await _amendmentBodyService.GetByAmentmentId(amendmentId);
            var body = bodies.FirstOrDefault(b => b.Id == id);
            if (body == null)
                return NotFound();

            var alreadyCreatedLanguageIds = bodies.Where(b => b.Id != id).Select(b => b.LanguageId).ToList();
            if (!ModelState.IsValid)
            {
                model.Languages = (await _languageDataService.GetAllAsync()).Where(l => alreadyCreatedLanguageIds.All(c => c != l.Id)).ToList();
                return View(model);
            }

            body = _mapper.Map(model, body);
            await _amendmentBodyService.UpdateAsync(body, User.UserId());

            return RedirectToAction(nameof(Index), "Amendment");
        }

        [Authorize(Roles = "System Administrator, Amendment Editor, Translator")]
        public async Task<ActionResult> Delete(int amendmentId, int id)
        {
            var body = await _amendmentBodyService.GetAsync(id);
            if (body == null)
                return NotFound();

            var model = _mapper.Map<AmendmentBodyDetailsViewModel>(body);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "System Administrator, Amendment Editor, Translator")]
        public async Task<ActionResult> Delete(int amendmentId, int id, IFormCollection collection)
        {
            var body = await _amendmentBodyService.GetAsync(id);
            if (body == null)
                return NotFound();

            await _amendmentBodyService.DeleteAsync(body, User.UserId());

            return RedirectToAction(nameof(Index), "Amendment");
        }
    }
}
