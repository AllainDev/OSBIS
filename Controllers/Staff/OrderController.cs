using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OSBIS.Common;
using OSBIS.Models.Enums;
using OSBIS.Models.ViewModels.Order;
using OSBIS.Services.Interfaces;

namespace OSBIS.Controllers.Staff
{
    /// <summary>
    /// Staff quản lý đơn hàng (Phase 4).
    /// Route: /Staff/Order
    /// </summary>
    [Authorize(Roles = "Admin,Staff")]
    [Area("Staff")]
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IPaymentService _paymentService;
        private readonly IShipmentService _shipmentService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(
            IOrderService orderService,
            IPaymentService paymentService,
            IShipmentService shipmentService,
            ILogger<OrderController> logger)
        {
            _orderService = orderService;
            _paymentService = paymentService;
            _shipmentService = shipmentService;
            _logger = logger;
        }

        // GET /Staff/Order
        [HttpGet]
        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 15,
            OrderStatus? status = null, string? search = null)
        {
            var paged = await _orderService.GetPagedAsync(pageNumber, pageSize, status, search);
            ViewBag.StatusFilter = status;
            ViewBag.Search = search;
            return View(paged);
        }

        // GET /Staff/Order/Detail/{id}
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var order = await _orderService.GetForStaffAsync(id);
            if (order == null) return NotFound();

            // Build items view
            var items = order.OrderDetails.Select(d => new OrderDetailItemViewModel
            {
                ProductId = d.ProductId,
                ProductName = d.ProductNameSnapshot,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice
            }).ToList();

            var vm = new OrderDetailViewModel
            {
                Order = order,
                Items = items,
                CanCancel = false // Staff không cancel ở đây
            };
            return View(vm);
        }

        // POST /Staff/Order/Confirm/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            var staffId = User.GetUserId() ?? 0;
            var result = await _orderService.ConfirmOrderAsync(id, staffId);

            if (result.Success) TempData["Success"] = result.Message;
            else TempData["Error"] = result.Message;

            return RedirectToAction(nameof(Detail), new { id });
        }

        // POST /Staff/Order/CreateShipment/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateShipment(int id)
        {
            var staffId = User.GetUserId() ?? 0;
            var result = await _shipmentService.CreateShipmentAsync(id, null, staffId);

            if (result.Success) TempData["Success"] = result.Message;
            else TempData["Error"] = result.Message;

            return RedirectToAction(nameof(Detail), new { id });
        }

        // POST /Staff/Order/ConfirmPayment/{paymentId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(int paymentId, int orderId, string billImageUrl)
        {
            var staffId = User.GetUserId() ?? 0;
            var result = await _paymentService.ConfirmBankTransferAsync(paymentId, billImageUrl ?? "", staffId);

            if (result.Success) TempData["Success"] = result.Message;
            else TempData["Error"] = result.Message;

            return RedirectToAction(nameof(Detail), new { id = orderId });
        }
    }
}
