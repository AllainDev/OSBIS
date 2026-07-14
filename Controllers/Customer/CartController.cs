using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OSBIS.Common;
using OSBIS.Services.Interfaces;

namespace OSBIS.Controllers.Customer
{
    /// <summary>
    /// Controller cho Customer quản lý giỏ hàng (Phase 3).
    /// Route: /Customer/Cart
    /// </summary>
    [Authorize]
    [Area("Customer")]
    public class CartController : Controller
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        // GET /Customer/Cart
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var summary = await _cartService.GetCartSummaryAsync();
            return View(summary);
        }

        // POST /Customer/Cart/Add (AJAX-friendly)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int quantity = 1)
        {
            var result = await _cartService.AddItemAsync(productId, quantity);
            if (result.Success)
                TempData["Success"] = result.Message;
            else
                TempData["Error"] = result.Message;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = result.Success, message = result.Message, count = result.CartItemCount });

            return RedirectToAction(nameof(Index));
        }

        // POST /Customer/Cart/UpdateQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            var result = await _cartService.UpdateQuantityAsync(cartItemId, quantity);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = result.Success, message = result.Message, count = result.CartItemCount });

            return RedirectToAction(nameof(Index));
        }

        // POST /Customer/Cart/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            var result = await _cartService.RemoveItemAsync(cartItemId);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = result.Success, message = result.Message, count = result.CartItemCount });

            return RedirectToAction(nameof(Index));
        }

        // GET /Customer/Cart/Count (header cart badge)
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Count()
        {
            var count = await _cartService.GetCartItemCountAsync();
            return Json(new { count });
        }
    }
}
