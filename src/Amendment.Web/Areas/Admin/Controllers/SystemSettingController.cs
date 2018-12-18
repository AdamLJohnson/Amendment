using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amendment.Model.DataModel;
using Amendment.Service;
using Microsoft.AspNetCore.Mvc;

namespace Amendment.Web.Areas.Admin.Controllers
{
    public class SystemSettingController : BaseController
    {
        private readonly ISystemSettingService _systemSettingService;

        public SystemSettingController(ISystemSettingService systemSettingService)
        {
            _systemSettingService = systemSettingService;
        }

        public async Task<IActionResult> Index()
        {
            var settings = await _systemSettingService.GetAllAsync();
            return View(settings);
        }

        [HttpPost]
        public async Task<IActionResult> Index(SystemSetting model)
        {
            var setting = await _systemSettingService.GetSettingAsync(model.Key);
            if (setting == null)
                return NotFound();

            setting.Value = model.Value;
            setting.LastUpdated = DateTime.UtcNow;
            setting.LastUpdatedBy = User.UserId();
            await _systemSettingService.UpdateAsync(setting, User.UserId());
            return Json(model);
        }
    }
}