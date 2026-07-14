using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OSBIS.Common;
using OSBIS.Models.Enums;
using OSBIS.Models.ViewModels.Order;
using OSBIS.Services.Interfaces;

namespace OSBIS.Controllers.Customer
{
    [Authorize]
    [Area("Customer")]
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IShipmentService _shipmentService;
        private readonly IProductService _productService;
        private readonly IReviewService _reviewService;

        public OrderController(
            IOrderService orderService,
            IShipmentService shipmentService,
            IProductService productService,
            IReviewService reviewService)
        {
            _orderService = orderService;
            _shipmentService = shipmentService;
            _productService = productService;
            _reviewService = reviewService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10)
        {
            var userId = User.GetUserId()!.Value;
            var paged = await _orderService.GetUserOrdersAsync(userId, pageNumber, pageSize);

            var vm = new OrderListViewModel { Orders = paged };
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var userId = User.GetUserId()!.Value;
            var order = await _orderService.GetOrderDetailAsync(id);
            if (order == null || order.UserId != userId) return NotFound();

            var productIds = order.OrderDetails.Select(d => d.ProductId).ToList();
            var products = await _productService.GetAllActiveAsync();
            var productMap = products.Where(p => productIds.Contains(p.ProductId))
                .ToDictionary(p => p.ProductId, p => p);

            var items = order.OrderDetails.Select(d =>
            {
                var product = productMap.GetValueOrDefault(d.ProductId);
                return new OrderDetailItemViewModel
                {
                    ProductId = d.ProductId,
                    ProductName = d.ProductNameSnapshot,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    ImageUrl = product?.ProductImages?.FirstOrDefault(i => i.IsPrimary == true)?.ImageUrl
                };
            }).ToList();

            var vm = new OrderDetailViewModel
            {
                Order = order,
                Items = items,
                CanCancel = order.OrderStatus == OrderStatus.Pending
                            || order.OrderStatus == OrderStatus.Confirmed
                            || order.OrderStatus == OrderStatus.Processing
            };

            // Đánh dấu sản phẩm đã review
            foreach (var item in vm.Items)
            {
                item.CanReview = order.OrderStatus == OrderStatus.Delivered || order.OrderStatus == OrderStatus.Completed;
            }
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = User.GetUserId()!.Value;
            var result = await _orderService.CancelOrderAsync(id, userId);

            if (result.Success) TempData["Success"] = result.Message;
            else TempData["Error"] = result.Message;

            return RedirectToAction(nameof(Detail), new { id });
        }

        // GET /Customer/Order/Tracking/{id}
        [HttpGet]
        public async Task<IActionResult> Tracking(int id)
        {
            var userId = User.GetUserId()!.Value;
            var order = await _orderService.GetOrderDetailAsync(id);
            if (order == null || order.UserId != userId) return NotFound();

            var shipment = await _shipmentService.GetByOrderIdAsync(order.OrderId);
            var trackings = shipment != null
                ? await _shipmentService.GetTrackingHistoryAsync(shipment.ShipmentId)
                : new List<Models.Entities.ShipmentTracking>();

            ViewBag.Order = order;
            ViewBag.Shipment = shipment;
            ViewBag.Trackings = trackings;

            return View();
        }
    }
}
