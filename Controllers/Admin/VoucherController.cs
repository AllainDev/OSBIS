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
    public class VoucherController : Controller
    {
        private readonly IUnitOfWork _uow;
        private readonly IAuditLogService _auditLogService;
        private readonly INotificationService _notificationService;
        private readonly IUserRepository _userRepository;

        public VoucherController(
            IUnitOfWork uow,
            IAuditLogService auditLogService,
            INotificationService notificationService,
            IUserRepository userRepository)
        {
            _uow = uow;
            _auditLogService = auditLogService;
            _notificationService = notificationService;
            _userRepository = userRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 20)
        {
            var paged = await _uow.Vouchers.GetPagedAsync(pageNumber, pageSize);
            return View(paged);
        }

        [HttpGet]
        public IActionResult Create() => View(new Voucher
        {
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            UsageLimit = 100,
            IsActive = true
        });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Voucher voucher, bool notifyAll = false)
        {
            if (!ModelState.IsValid) return View(voucher);

            await _uow.BeginTransactionAsync();
            await _uow.Vouchers.AddAsync(voucher);
            await _uow.CommitTransactionAsync();

            await _auditLogService.LogAsync(User.GetUserId(), null, AuditAction.VoucherCreated,
                $"Created voucher {voucher.VoucherCode}", true, "Admin/Voucher", "Create");

            if (notifyAll)
            {
                var customers = await _userRepository.GetUsersByRoleAsync("Customer");
                foreach (var u in customers)
                {
                    await _notificationService.NotifyVoucherAvailableAsync(u.UserId, voucher);
                }
            }

            TempData["Success"] = "Tạo voucher thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var v = await _uow.Vouchers.GetByIdAsync(id);
            if (v == null) return NotFound();
            return View(v);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Voucher voucher)
        {
            if (id != voucher.VoucherId) return BadRequest();

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

            TempData["Success"] = "Cập nhật voucher.";
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

        [HttpGet]
        public async Task<IActionResult> UsageHistory(int id)
        {
            var voucher = await _uow.Vouchers.GetByIdAsync(id);
            if (voucher == null) return NotFound();
            var usages = await _uow.VoucherUsages.GetByVoucherIdAsync(id);
            ViewBag.Voucher = voucher;
            return View(usages);
        }
    }
}
