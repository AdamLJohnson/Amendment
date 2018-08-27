using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Amendment.Model.DataModel;
using Amendment.Model.ViewModel.ScreenView;
using Amendment.Service;
using Amendment.Service.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Amendment.Web.Models;
using AutoMapper;
using Microsoft.CodeAnalysis.Host;

namespace Amendment.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IAmendmentService _amendmentService;
        private readonly IMapper _mapper;
        private readonly IReadOnlyDataService<Language> _languageService;

        public HomeController(IAmendmentService amendmentService, IMapper mapper, IReadOnlyDataService<Language> languageService)
        {
            _amendmentService = amendmentService;
            _mapper = mapper;
            _languageService = languageService;
        }

        public async Task<IActionResult> Index()
        {
            var languageList = await _languageService.GetAllAsync();

            var amendment = await _amendmentService.GetLiveAsync() ?? new Model.DataModel.Amendment();
            var amendmentBody = amendment.AmendmentBodies;

            var amendmentModel = _mapper.Map<AmendmentViewViewModel>(amendment);
            var amendmentBodyModel = _mapper.Map<List<AmendmentBodyViewViewModel>>(amendmentBody);

            var model = new HomeScreenViewViewModel()
            {
                Languages = languageList,
                Amendment = amendmentModel,
                AmendmentBodies = amendmentBodyModel
            };
            return View(model);
        }

        public async Task<IActionResult> View(string id) //id = languageName
        {
            var languageList = await _languageService.GetAllAsync();
            var language = languageList.FirstOrDefault(l => l.LanguageName?.ToLower() == id?.ToLower());
            if (language == null)
                RedirectToAction(nameof(Index));

            var amendment = await _amendmentService.GetLiveAsync() ?? new Model.DataModel.Amendment();
            var amendmentBody = amendment.AmendmentBodies.FirstOrDefault(b => b.IsLive && b.Language?.LanguageName?.ToLower() == id?.ToLower()) ?? new AmendmentBody();

            var amendmentModel = _mapper.Map<AmendmentViewViewModel>(amendment);
            var amendmentBodyModel = _mapper.Map<AmendmentBodyViewViewModel>(amendmentBody);

            var model = new ScreenViewViewModel()
            {
                Language = language,
                Amendment = amendmentModel,
                AmendmentBody = amendmentBodyModel
            };
            return View(model);
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
