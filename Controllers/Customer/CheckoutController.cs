using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OSBIS.Common;
using OSBIS.Models.ViewModels.Checkout;
using OSBIS.Services.Helpers;
using OSBIS.Services.Interfaces;

namespace OSBIS.Controllers.Customer
{
    /// <summary>
    /// Controller cho Checkout flow (Phase 3).
    /// Route: /Customer/Checkout
    /// </summary>
    [Authorize]
    [Area("Customer")]
    public class CheckoutController : Controller
    {
        private readonly ICartService _cartService;
        private readonly IOrderService _orderService;
        private readonly IVoucherService _voucherService;
        private readonly ISystemConfigService _configService;

        public CheckoutController(
            ICartService cartService,
            IOrderService orderService,
            IVoucherService voucherService,
            ISystemConfigService configService)
        {
            _cartService = cartService;
            _orderService = orderService;
            _voucherService = voucherService;
            _configService = configService;
        }

        // GET /Customer/Checkout
        [HttpGet]
        public async Task<IActionResult> Index(string? voucherCode)
        {
            var cart = await _cartService.GetCartSummaryAsync();
            if (cart.IsEmpty)
            {
                TempData["Warning"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index", "Cart");
            }

            var userId = User.GetUserId()!.Value;
            var subTotal = cart.SubTotal;
            var totalWeight = cart.TotalWeight;

            // Tính shipping preview
            var feePerKg = await _configService.GetDecimalAsync(ShippingCalculator.ShippingFeePerKgKey) ?? 25000m;
            var freeThreshold = await _configService.GetDecimalAsync(ShippingCalculator.FreeShipThresholdKey) ?? 500000m;
            var minFee = await _configService.GetDecimalAsync(ShippingCalculator.MinShippingFeeKey) ?? 30000m;
            var shippingFee = ShippingCalculator.Calculate(subTotal, totalWeight, feePerKg, freeThreshold, minFee);

            var vm = new CheckoutViewModel
            {
                Cart = cart,
                VoucherCode = voucherCode,
                ShippingFee = shippingFee,
                TotalAmount = subTotal + shippingFee,
                TotalWeight = totalWeight,
                BankName = await _configService.GetStringAsync("BankName"),
                BankAccountNumber = await _configService.GetStringAsync("BankAccountNumber"),
                BankAccountName = await _configService.GetStringAsync("BankAccountName"),
            };

            // Preview voucher nếu có
            if (!string.IsNullOrWhiteSpace(voucherCode))
            {
                var validation = await _voucherService.ValidateAsync(voucherCode, userId, subTotal);
                if (validation.IsValid && validation.Voucher != null)
                {
                    var discount = _voucherService.CalculateDiscount(validation.Voucher, subTotal, shippingFee);
                    vm.VoucherDiscountPreview = discount;
                    vm.DiscountAmount = discount;
                    vm.TotalAmount = subTotal + shippingFee - discount;
                }
                else
                {
                    vm.VoucherError = validation.ErrorMessage;
                }
            }

            return View(vm);
        }

        // POST /Customer/Checkout/PlaceOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel dto)
        {
            if (!ModelState.IsValid)
            {
                var cart = await _cartService.GetCartSummaryAsync();
                dto.Cart = cart;
                dto.BankName = await _configService.GetStringAsync("BankName");
                dto.BankAccountNumber = await _configService.GetStringAsync("BankAccountNumber");
                dto.BankAccountName = await _configService.GetStringAsync("BankAccountName");
                dto.TotalWeight = cart.TotalWeight;
                return View("Index", dto);
            }

            var userId = User.GetUserId()!.Value;
            var result = await _orderService.PlaceOrderAsync(userId, dto);

            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Success), new { id = result.Order!.OrderId });
        }

        // GET /Customer/Checkout/Success/{id}
        [HttpGet]
        public async Task<IActionResult> Success(int id)
        {
            var order = await _orderService.GetOrderDetailAsync(id);
            if (order == null || order.UserId != User.GetUserId()) return NotFound();

            ViewBag.BankName = await _configService.GetStringAsync("BankName");
            ViewBag.BankAccountNumber = await _configService.GetStringAsync("BankAccountNumber");
            ViewBag.BankAccountName = await _configService.GetStringAsync("BankAccountName");

            return View(order);
        }
    }
}
