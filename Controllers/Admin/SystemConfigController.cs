using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OSBIS.Common;
using OSBIS.Models.Entities;
using OSBIS.Models.Enums;
using OSBIS.Repositories.Interfaces;
using OSBIS.Services.Interfaces;

namespace OSBIS.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    public class SystemConfigController : Controller
    {
        private readonly IUnitOfWork _uow;
        private readonly IAuditLogService _auditLogService;

        public SystemConfigController(IUnitOfWork uow, IAuditLogService auditLogService)
        {
            _uow = uow;
            _auditLogService = auditLogService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var list = await _uow.SystemConfigs.GetAllAsync();
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string key)
        {
            var config = await _uow.SystemConfigs.GetByKeyAsync(key);
            if (config == null) return NotFound();
            return View(config);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string key, SystemConfig model)
        {
            if (key != model.ConfigKey) return BadRequest();

            var config = await _uow.SystemConfigs.GetByKeyAsync(key);
            if (config == null) return NotFound();

            config.ConfigValue = model.ConfigValue;
            config.Description = model.Description;
            config.UpdatedBy = User.GetUserId();

            await _uow.BeginTransactionAsync();
            _uow.SystemConfigs.Update(config);
            await _uow.CommitTransactionAsync();

            await _auditLogService.LogAsync(User.GetUserId(), null, AuditAction.SystemConfigChanged,
                $"Config {key} = {model.ConfigValue}", true, "Admin/SystemConfig", "Edit");

            TempData["Success"] = "Cập nhật cấu hình.";
            return RedirectToAction(nameof(Index));
        }
    }
}
