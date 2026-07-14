using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OSBIS.Common;
using OSBIS.Services.Interfaces;

namespace OSBIS.Controllers.Staff
{
    /// <summary>
    /// Staff xem thông báo cá nhân.
    /// Staff được phép:
    ///   - Xem / đánh dấu đã đọc notification của mình
    /// Staff KHÔNG được:
    ///   - SendToUser, Broadcast (chỉ Admin mới gửi broadcast)
    /// </summary>
    [Authorize(Roles = "Admin,Staff")]
    [Area("Staff")]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 20)
        {
            var userId = User.GetUserId() ?? 0;
            var paged = await _notificationService.GetByUserAsync(userId, pageNumber, pageSize);
            return View("~/Views/Staff/Notification/Index.cshtml", paged);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id)
        {
            var userId = User.GetUserId() ?? 0;
            await _notificationService.MarkAsReadAsync(id, userId);
            return RedirectToAction(nameof(Index));
        }
    }
}