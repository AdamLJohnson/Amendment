using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amendment.Model.ViewModel.AmendmentBody;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Amendment.Web.Controllers
{
    public class AmendmentBodyController : Controller
    {
        public async Task<ActionResult> Index(int amendmentId)
        {
            return View();
        }

        public async Task<ActionResult> Detail(int amendmentId, int id)
        {
            return View(new AmendmentBodyDetailsViewModel());
        }

        public async Task<ActionResult> Create(int amendmentId)
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(AmendmentBodyCreateViewModel model, int amendmentId)
        {
            return RedirectToAction(nameof(Index));
        }

        public async Task<ActionResult> Edit(int amendmentId, int id)
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int amendmentId, int id, AmendmentBodyEditViewModel model)
        {
            return RedirectToAction(nameof(Index));
        }

        public async Task<ActionResult> Delete(int amendmentId, int id)
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int amendmentId, int id, IFormCollection collection)
        {
            return RedirectToAction(nameof(Index));
        }
    }
}
