using Microsoft.AspNetCore.Mvc;
using OSBIS.Common;
using OSBIS.Services.Interfaces;

namespace OSBIS.ViewComponents
{
    /// <summary>
    /// ViewComponent hiển thị notification bell + dropdown trên navbar.
    /// </summary>
    public class NotificationViewComponent : ViewComponent
    {
        private readonly INotificationService _notificationService;

        public NotificationViewComponent(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = HttpContext.User.GetUserId() ?? 0;
            if (userId == 0)
                return View(new NotificationBellModel { IsAuth = false });

            var unread = await _notificationService.GetUnreadCountAsync(userId);
            var latest = await _notificationService.GetLatestAsync(userId, 5);

            return View(new NotificationBellModel
            {
                IsAuth = true,
                UnreadCount = unread,
                Latest = latest.ToList()
            });
        }
    }

    public class NotificationBellModel
    {
        public bool IsAuth { get; set; }
        public int UnreadCount { get; set; }
        public List<OSBIS.Models.Entities.Notification> Latest { get; set; } = new();
    }
}
