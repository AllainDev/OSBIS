using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OSBIS.Common;
using OSBIS.Models.Enums;
using OSBIS.Services.Interfaces;

namespace OSBIS.Controllers.Shipper
{
    /// <summary>
    /// Shipper cập nhật trạng thái giao hàng (Phase 4).
    /// </summary>
    [Authorize(Roles = "Admin,Delivery")]
    [Area("Shipper")]
    public class ShipmentController : Controller
    {
        private readonly IShipmentService _shipmentService;
        private readonly ILogger<ShipmentController> _logger;

        public ShipmentController(IShipmentService shipmentService, ILogger<ShipmentController> logger)
        {
            _shipmentService = shipmentService;
            _logger = logger;
        }

        // GET /Shipper/Shipment
        [HttpGet]
        public async Task<IActionResult> Index(ShipmentStatus? status = null)
        {
            var shipperId = User.GetUserId() ?? 0;
            var list = await _shipmentService.GetByShipperAsync(shipperId, status);
            ViewBag.StatusFilter = status;
            return View(list);
        }

        // GET /Shipper/Shipment/Detail/{id}
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var shipment = await _shipmentService.GetWithTrackingsAsync(id);
            if (shipment == null) return NotFound();

            // Check quyền
            var shipperId = User.GetUserId() ?? 0;
            if (shipment.AssignedShipperId != shipperId && !User.IsInRole("Admin"))
                return Forbid();

            return View(shipment);
        }

        // POST /Shipper/Shipment/UpdateStatus/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, ShipmentStatus newStatus, string? location, string? note)
        {
            var shipperId = User.GetUserId() ?? 0;
            var result = await _shipmentService.UpdateStatusAsync(id, newStatus, location, note, shipperId);

            if (result.Success) TempData["Success"] = result.Message;
            else TempData["Error"] = result.Message;

            return RedirectToAction(nameof(Detail), new { id });
        }

        // POST /Shipper/Shipment/ConfirmCOD/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmCOD(int id)
        {
            var shipperId = User.GetUserId() ?? 0;
            var result = await _shipmentService.ConfirmCODReceivedAsync(id, shipperId);

            if (result.Success) TempData["Success"] = result.Message;
            else TempData["Error"] = result.Message;

            return RedirectToAction(nameof(Detail), new { id });
        }
    }
}
