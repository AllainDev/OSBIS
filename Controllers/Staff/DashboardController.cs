using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OSBIS.Common;
using OSBIS.Services.Interfaces;

namespace OSBIS.Controllers.Staff
{
    [Authorize(Roles = "Admin,Staff")]
    [Area("Staff")]
    public class DashboardController : Controller
    {
        private readonly IReportService _reportService;
        private readonly INotificationService _notificationService;

        public DashboardController(IReportService reportService, INotificationService notificationService)
        {
            _reportService = reportService;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.GetUserId();
            if (userId.HasValue)
            {
                await _notificationService.GenerateExpiringBatchNotificationsAsync(userId.Value);
            }

            var data = await _reportService.GetDashboardDataAsync();

            // Order status pie
            var summary = data.OrderSummary;
            ViewBag.PieLabels = System.Text.Json.JsonSerializer.Serialize(
                new[] { "Chờ xác nhận", "Đã xác nhận", "Đang xử lý", "Đã gửi", "Đã giao", "Hoàn thành", "Đã hủy" });
            ViewBag.PieValues = System.Text.Json.JsonSerializer.Serialize(new[]
            {
                summary.Pending, summary.Confirmed, summary.Processing,
                summary.Shipped, summary.Delivered, summary.Completed,
                summary.Cancelled
            });

            // Trend chart (last 30 days)
            var labels = data.Last30Days.Select(x => x.Date.ToString("dd/MM")).ToArray();
            var orders = data.Last30Days.Select(x => x.OrderCount).ToArray();
            var revenue = data.Last30Days.Select(x => x.Revenue).ToArray();

            ViewBag.ChartLabels = System.Text.Json.JsonSerializer.Serialize(labels);
            ViewBag.ChartOrders = System.Text.Json.JsonSerializer.Serialize(orders);
            ViewBag.ChartRevenue = System.Text.Json.JsonSerializer.Serialize(revenue);

            ViewBag.TopProducts = data.TopProducts;

            return View(data);
        }
    }
}