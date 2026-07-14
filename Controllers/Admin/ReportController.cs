using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OSBIS.Services.Interfaces;

namespace OSBIS.Controllers.Admin
{
    /// <summary>
    /// Admin xem báo cáo thống kê (Phase 5).
    /// </summary>
    [Authorize(Roles = "Admin,Staff")]
    [Area("Admin")]
    public class ReportController : Controller
    {
        private readonly IReportService _reportService;
        public ReportController(IReportService reportService) { _reportService = reportService; }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var data = await _reportService.GetDashboardDataAsync();
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Revenue(DateTime? fromDate, DateTime? toDate)
        {
            var from = fromDate ?? DateTime.UtcNow.AddDays(-29);
            var to = toDate ?? DateTime.UtcNow.AddDays(1);
            var data = await _reportService.GetRevenueByDateRangeAsync(from, to);
            ViewBag.FromDate = from;
            ViewBag.ToDate = to;
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Inventory()
        {
            var data = await _reportService.GetInventoryStatusAsync();
            return View(data);
        }
    }
}
