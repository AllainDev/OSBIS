using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OSBIS.Common;
using OSBIS.Services.Interfaces;

namespace OSBIS.Controllers.Customer
{
    /// <summary>
    /// Customer đánh giá sản phẩm sau khi đã mua (Phase 4).
    /// </summary>
    [Authorize]
    [Area("Customer")]
    public class ReviewController : Controller
    {
        private readonly IReviewService _reviewService;
        private readonly IOrderService _orderService;

        public ReviewController(IReviewService reviewService, IOrderService orderService)
        {
            _reviewService = reviewService;
            _orderService = orderService;
        }

        // GET /Customer/Review/Create?orderId=1&productId=2
        [HttpGet]
        public async Task<IActionResult> Create(int orderId, int productId)
        {
            var userId = User.GetUserId() ?? 0;
            var canReview = await _reviewService.CanReviewAsync(userId, orderId, productId);
            if (!canReview)
            {
                TempData["Error"] = "Bạn không thể đánh giá sản phẩm này.";
                return RedirectToAction("Detail", "Order", new { id = orderId });
            }

            var order = await _orderService.GetOrderDetailAsync(orderId);
            ViewBag.OrderCode = order?.OrderCode;
            ViewBag.ProductName = order?.OrderDetails.FirstOrDefault(d => d.ProductId == productId)?.ProductNameSnapshot;
            ViewBag.OrderId = orderId;
            ViewBag.ProductId = productId;
            return View();
        }

        // POST /Customer/Review/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int orderId, int productId, byte rating, string? comment)
        {
            var userId = User.GetUserId() ?? 0;
            var result = await _reviewService.CreateReviewAsync(userId, orderId, productId, rating, comment);

            if (result.Success) TempData["Success"] = result.Message;
            else TempData["Error"] = result.Message;

            return RedirectToAction("Detail", "Order", new { id = orderId });
        }

        // GET /Customer/Review/Product/{productId}
        [HttpGet]
        public async Task<IActionResult> ByProduct(int productId, int pageNumber = 1, int pageSize = 10)
        {
            var paged = await _reviewService.GetByProductAsync(productId, pageNumber, pageSize);
            return View(paged);
        }
    }
}
