using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OSBIS.Common;
using OSBIS.Models.Enums;
using OSBIS.Services.Interfaces;

namespace OSBIS.Controllers.Staff
{
    /// <summary>
    /// Staff quản lý vận đơn (Phase 4).
    /// </summary>
    [Authorize(Roles = "Admin,Staff")]
    [Area("Staff")]
    public class ShipmentController : Controller
    {
        private readonly IShipmentService _shipmentService;
        private readonly ILogger<ShipmentController> _logger;

        public ShipmentController(IShipmentService shipmentService, ILogger<ShipmentController> logger)
        {
            _shipmentService = shipmentService;
            _logger = logger;
        }

        // GET /Staff/Shipment
        [HttpGet]
        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 15, ShipmentStatus? status = null)
        {
            var paged = await _shipmentService.GetPagedAsync(pageNumber, pageSize, status);
            ViewBag.StatusFilter = status;
            return View(paged);
        }

        // GET /Staff/Shipment/Detail/{id}
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var shipment = await _shipmentService.GetWithTrackingsAsync(id);
            if (shipment == null) return NotFound();
            return View(shipment);
        }

        // POST /Staff/Shipment/AssignShipper/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignShipper(int id, int shipperId)
        {
            var staffId = User.GetUserId() ?? 0;
            var result = await _shipmentService.AssignShipperAsync(id, shipperId, staffId);

            if (result.Success) TempData["Success"] = result.Message;
            else TempData["Error"] = result.Message;

            return RedirectToAction(nameof(Detail), new { id });
        }
    }
}
