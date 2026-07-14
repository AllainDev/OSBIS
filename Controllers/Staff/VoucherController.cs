using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OSBIS.Common;
using OSBIS.Models.Entities;
using OSBIS.Models.Enums;
using OSBIS.Repositories.Interfaces;
using OSBIS.Services.Interfaces;

namespace OSBIS.Controllers.Staff
{
    /// <summary>
    /// Staff quản lý voucher (CRUD cơ bản).
    /// Staff được phép:
    ///   - Xem / tạo / sửa / vô hiệu voucher
    /// Staff KHÔNG được:
    ///   - Gửi broadcast notification
    ///   - Quản lý User / SystemConfig
    /// </summary>
    [Authorize(Roles = "Admin,Staff")]
    [Area("Staff")]
    public class VoucherController : Controller
    {
        private readonly IUnitOfWork _uow;
        private readonly IAuditLogService _auditLogService;

        public VoucherController(IUnitOfWork uow, IAuditLogService auditLogService)
        {
            _uow = uow;
            _auditLogService = auditLogService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 20)
        {
            var paged = await _uow.Vouchers.GetPagedAsync(pageNumber, pageSize);
            return View("~/Views/Staff/Voucher/Index.cshtml", paged);
        }

        [HttpGet]
        public IActionResult Create() => View("~/Views/Staff/Voucher/Create.cshtml", new Voucher
        {
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            UsageLimit = 100,
            IsActive = true
        });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Voucher voucher)
        {
            if (!ModelState.IsValid) return View("~/Views/Staff/Voucher/Create.cshtml", voucher);

            await _uow.BeginTransactionAsync();
            await _uow.Vouchers.AddAsync(voucher);
            await _uow.CommitTransactionAsync();

            await _auditLogService.LogAsync(User.GetUserId(), null, AuditAction.VoucherCreated,
                $"Created voucher {voucher.VoucherCode}", true, "Staff/Voucher", "Create");

            TempData["Success"] = "Tạo voucher thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var v = await _uow.Vouchers.GetByIdAsync(id);
            if (v == null) return NotFound();
            return View("~/Views/Staff/Voucher/Edit.cshtml", v);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Voucher voucher)
        {
            if (id != voucher.VoucherId) return BadRequest();
            if (!ModelState.IsValid) return View("~/Views/Staff/Voucher/Edit.cshtml", voucher);

            var v = await _uow.Vouchers.GetByIdAsync(id);
            if (v == null) return NotFound();

            v.VoucherCode = voucher.VoucherCode;
            v.DiscountType = voucher.DiscountType;
            v.DiscountValue = voucher.DiscountValue;
            v.MinOrderValue = voucher.MinOrderValue;
            v.MaxDiscountAmount = voucher.MaxDiscountAmount;
            v.StartDate = voucher.StartDate;
            v.EndDate = voucher.EndDate;
            v.UsageLimit = voucher.UsageLimit;
            v.IsActive = voucher.IsActive;

            await _uow.BeginTransactionAsync();
            _uow.Vouchers.Update(v);
            await _uow.CommitTransactionAsync();

            TempData["Success"] = "Đã cập nhật voucher.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var v = await _uow.Vouchers.GetByIdAsync(id);
            if (v == null) return NotFound();
            v.IsActive = false;

            await _uow.BeginTransactionAsync();
            _uow.Vouchers.Update(v);
            await _uow.CommitTransactionAsync();

            TempData["Success"] = "Đã vô hiệu voucher.";
            return RedirectToAction(nameof(Index));
        }
    }
}