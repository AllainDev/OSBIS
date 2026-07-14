using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OSBIS.Common;
using OSBIS.Models.Enums;
using OSBIS.Repositories.Interfaces;
using OSBIS.Services.Interfaces;

namespace OSBIS.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly IUserRepository _userRepository;

        public NotificationController(INotificationService notificationService, IUserRepository userRepository)
        {
            _notificationService = notificationService;
            _userRepository = userRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 20)
        {
            var userId = User.GetUserId() ?? 0;
            var paged = await _notificationService.GetByUserAsync(userId, pageNumber, pageSize);
            return View(paged);
        }

        [HttpGet]
        public IActionResult SendToUser() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendToUser(int userId, string title, string message)
        {
            await _notificationService.CreateAsync(userId, "AdminMessage", title, message);
            TempData["Success"] = "Đã gửi notification.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Broadcast() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Broadcast(string title, string message)
        {
            var users = await _userRepository.GetUsersByRoleAsync("Customer");
            foreach (var u in users)
            {
                await _notificationService.CreateAsync(u.UserId, "Broadcast", title, message);
            }
            TempData["Success"] = $"Đã gửi tới {users.Count()} khách hàng.";
            return RedirectToAction(nameof(Index));
        }
    }
}
