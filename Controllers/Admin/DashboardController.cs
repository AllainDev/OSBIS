using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OSBIS.Common;
using OSBIS.Services.Interfaces;

namespace OSBIS.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    public class DashboardController : Controller
    {
        private readonly IReportService _reportService;
        private readonly IAuditLogService _auditLogService;
        private readonly INotificationService _notificationService;

        public DashboardController(IReportService reportService, IAuditLogService auditLogService, INotificationService notificationService)
        {
            _reportService = reportService;
            _auditLogService = auditLogService;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.GetUserId();
            if (userId.HasValue)
            {
                await _notificationService.GenerateExpiringBatchNotificationsAsync(userId.Value);
            }

            ViewBag.UserName = User.GetFullName() ?? User.Identity?.Name;
            ViewBag.Role = User.GetRole();

            // Pull full dashboard data from report service
            var data = await _reportService.GetDashboardDataAsync();

            // Recent activity (audit log)
            var recentLogs = await _auditLogService.GetRecentLogsAsync(50);
            ViewBag.RecentLogs = recentLogs.OrderByDescending(l => l.CreatedAt).Take(20).ToList();

            // Pass-through chart-ready series as JSON for Chart.js
            var labels = data.Last30Days.Select(x => x.Date.ToString("dd/MM")).ToArray();
            var revenue = data.Last30Days.Select(x => x.Revenue).ToArray();
            var orders = data.Last30Days.Select(x => x.OrderCount).ToArray();

            ViewBag.ChartLabels = System.Text.Json.JsonSerializer.Serialize(labels);
            ViewBag.ChartRevenue = System.Text.Json.JsonSerializer.Serialize(revenue);
            ViewBag.ChartOrders = System.Text.Json.JsonSerializer.Serialize(orders);

            // Order status pie
            var summary = data.OrderSummary;
            ViewBag.PieLabels = System.Text.Json.JsonSerializer.Serialize(
                new[] { "Chờ xác nhận", "Đã xác nhận", "Đang xử lý", "Đã gửi", "Đã giao", "Hoàn thành", "Đã hủy", "Hoàn tiền" });
            ViewBag.PieValues = System.Text.Json.JsonSerializer.Serialize(new[]
            {
                summary.Pending, summary.Confirmed, summary.Processing,
                summary.Shipped, summary.Delivered, summary.Completed,
                summary.Cancelled, summary.Refunded
            });

            // Top products
            ViewBag.TopProducts = data.TopProducts;

            return View(data);
        }
    }
}