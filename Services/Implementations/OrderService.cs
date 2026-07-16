using OSBIS.Common;
using OSBIS.Models.Entities;
using OSBIS.Models.Enums;
using OSBIS.Models.ViewModels.Checkout;
using OSBIS.Repositories.Interfaces;
using OSBIS.Services.Helpers;
using OSBIS.Services.Interfaces;
using Serilog;

namespace OSBIS.Services.Implementations
{
    /// <summary>
    /// Service quản lý Order — Phase 3 logic phức tạp nhất:
    /// - PlaceOrderAsync: validation + snapshot + reserve (đã giữ) + transaction
    /// - CancelOrderAsync: hoàn ReservedQuantity + voucher
    /// - Phase 4: Staff operations
    /// - Phase 5: tích hợp NotificationService + EmailService.
    ///
    /// FIX đã áp dụng:
    /// - PlaceOrderAsync: gọi NotifyOrderPlacedAsync + SendOrderConfirmationAsync sau commit
    /// - CancelOrderAsync: KHÔNG cho phép cancel khi OrderStatus.Confirmed (đã thanh toán → cần refund flow riêng)
    /// </summary>
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _uow;
        private readonly IVoucherService _voucherService;
        private readonly IPaymentService _paymentService;
        private readonly IAuditLogService _auditLogService;
        private readonly ICartService _cartService;
        private readonly ISystemConfigService _configService;
        private readonly IProductRepository _productRepository;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;

        public OrderService(
            IUnitOfWork uow,
            IVoucherService voucherService,
            IPaymentService paymentService,
            IAuditLogService auditLogService,
            ICartService cartService,
            ISystemConfigService configService,
            IProductRepository productRepository,
            INotificationService notificationService,
            IEmailService emailService)
        {
            _uow = uow;
            _voucherService = voucherService;
            _paymentService = paymentService;
            _auditLogService = auditLogService;
            _cartService = cartService;
            _configService = configService;
            _productRepository = productRepository;
            _notificationService = notificationService;
            _emailService = emailService;
        }

        // ============================================================
        // PlaceOrderAsync
        // ============================================================
        public async Task<PlaceOrderResult> PlaceOrderAsync(int userId, CheckoutViewModel dto)
        {
            Order? order = null;
            try
            {
                await _uow.BeginTransactionAsync();

                // 1. Load cart
                var cart = await _uow.Carts.GetByUserIdWithItemsAsync(userId);
                if (cart == null || !cart.CartItems.Any())
                    return new PlaceOrderResult { Success = false, Message = "Giỏ hàng trống." };

                // 2. Build OrderDetails + check stock + tính SubTotal / TotalWeight
                var orderDetails = new List<OrderDetail>();
                decimal subTotal = 0m, totalWeight = 0m;

                foreach (var item in cart.CartItems)
                {
                    var product = await _uow.Products.GetByIdAsync(item.ProductId);
                    if (product == null)
                        return new PlaceOrderResult { Success = false, Message = "Sản phẩm không tồn tại." };

                    var available = product.GetAvailableStock();
                    if (available < item.Quantity)
                    {
                        await _uow.RollbackTransactionAsync();
                        return new PlaceOrderResult
                        {
                            Success = false,
                            Message = $"Sản phẩm '{product.ProductName}' chỉ còn {available} trong kho."
                        };
                    }

                    orderDetails.Add(new OrderDetail
                    {
                        ProductId = product.ProductId,
                        ProductNameSnapshot = product.ProductName,
                        Quantity = item.Quantity,
                        UnitPrice = product.UnitPrice
                    });

                    subTotal += product.UnitPrice * item.Quantity;
                    totalWeight += product.Weight * item.Quantity;
                }

                // 3. Validate + calculate voucher (nếu có)
                Voucher? voucher = null;
                decimal discount = 0m;
                if (!string.IsNullOrWhiteSpace(dto.VoucherCode))
                {
                    var validation = await _voucherService.ValidateAsync(dto.VoucherCode, userId, subTotal);
                    if (!validation.IsValid)
                    {
                        await _uow.RollbackTransactionAsync();
                        return new PlaceOrderResult { Success = false, Message = validation.ErrorMessage };
                    }
                    voucher = validation.Voucher;
                }

                // 4. Shipping fee
                var shippingFee = await ShippingCalculatorHelper(subTotal, totalWeight);

                // 5. Calculate discount (sau khi đã biết shippingFee cho FreeShipping)
                if (voucher != null)
                    discount = _voucherService.CalculateDiscount(voucher, subTotal, shippingFee);

                var totalAmount = Math.Max(0, subTotal + shippingFee - discount);

                // 6. Tạo Order
                var orderCode = await GenerateOrderCodeAsync();
                var fullAddress = $"{dto.ReceiverName} | {dto.ReceiverPhone} | {dto.ShippingAddress}";
                if (!string.IsNullOrWhiteSpace(dto.Note))
                    fullAddress += $" | Ghi chú: {dto.Note}";

                order = new Order
                {
                    UserId = userId,
                    VoucherId = voucher?.VoucherId,
                    OrderCode = orderCode,
                    OrderDate = DateTime.UtcNow,
                    SubTotal = subTotal,
                    ShippingFee = shippingFee,
                    DiscountAmount = discount,
                    TotalAmount = totalAmount,
                    ShippingAddress = fullAddress,
                    OrderStatus = OrderStatus.Pending,
                    OrderDetails = orderDetails,
                    UpdatedAt = DateTime.UtcNow
                };

                await _uow.Orders.AddAsync(order);

                // 7. Tạo Payment (Pending)
                var payment = _paymentService.CreatePaymentEntity(
                    order.OrderId,
                    (int)dto.PaymentMethod,
                    totalAmount,
                    null);
                await _uow.Payments.AddAsync(payment);

                // 8. UseVoucher (nếu có)
                if (voucher != null)
                    await _voucherService.UseAsync(voucher.VoucherId, userId, order.OrderId);

                // 9. Clear cart items (giữ ReservedQuantity cho đến khi thanh toán thành công ở PaymentService)
                foreach (var item in cart.CartItems.ToList())
                    _uow.Carts.RemoveCartItem(item);

                await _uow.CommitTransactionAsync();

                // 10. AuditLog
                await _auditLogService.LogAsync(
                    userId, null,
                    AuditAction.OrderPlaced,
                    description: $"Order placed: {orderCode} - {totalAmount:N0}đ",
                    isSuccess: true,
                    controller: "Checkout",
                    actionName: "PlaceOrder"
                );

                Log.Information("Order {OrderCode} placed by user {UserId}, total {Total}",
                    orderCode, userId, totalAmount);

                // ✅ FIX: Gọi NotificationService + EmailService sau khi commit thành công (fire-and-forget)
                _ = SafeNotifyAsync(() => _notificationService.NotifyOrderPlacedAsync(order),
                    $"Notify placed for order {orderCode}");
                _ = SafeEmailAsync(() => _emailService.SendOrderConfirmationAsync(order),
                    $"Email confirmation for order {orderCode}");

                return new PlaceOrderResult { Success = true, Order = order, Message = "Đặt hàng thành công." };
            }
            catch (Exception ex)
            {
                await _uow.RollbackTransactionAsync();
                Log.Error(ex, "PlaceOrder failed for user {UserId}", userId);
                return new PlaceOrderResult { Success = false, Message = "Đã xảy ra lỗi khi đặt hàng. Vui lòng thử lại." };
            }
        }

        private async Task<decimal> ShippingCalculatorHelper(decimal subTotal, decimal totalWeight)
        {
            var feePerKg = await _configService.GetDecimalAsync(ShippingCalculator.ShippingFeePerKgKey) ?? 25000m;
            var freeThreshold = await _configService.GetDecimalAsync(ShippingCalculator.FreeShipThresholdKey) ?? 500000m;
            var minFee = await _configService.GetDecimalAsync(ShippingCalculator.MinShippingFeeKey) ?? 30000m;

            return ShippingCalculator.Calculate(subTotal, totalWeight, feePerKg, freeThreshold, minFee);
        }

        private async Task<string> GenerateOrderCodeAsync()
        {
            var generator = new OrderCodeGenerator(_uow);
            return await generator.GenerateAsync();
        }

        // ============================================================
        // CancelOrderAsync
        // ============================================================
        public async Task<CancelOrderResult> CancelOrderAsync(int orderId, int userId)
        {
            try
            {
                await _uow.BeginTransactionAsync();

                var order = await _uow.Orders.GetWithDetailsAsync(orderId);
                if (order == null)
                {
                    await _uow.RollbackTransactionAsync();
                    return new CancelOrderResult { Success = false, Message = "Không tìm thấy đơn hàng." };
                }

                if (order.UserId != userId)
                {
                    await _uow.RollbackTransactionAsync();
                    return new CancelOrderResult { Success = false, Message = "Bạn không có quyền hủy đơn này." };
                }

                // ✅ FIX: Chỉ cho phép cancel khi chưa thanh toán.
                // Plan yêu cầu: "Chỉ Pending/Processing (trước shipper pickup)".
                // Confirmed = đã thanh toán → KHÔNG cancel tự do, cần refund flow riêng (qua Staff).
                // Processing ở đây = "đang chuẩn bị hàng trước khi shipper pickup" → vẫn cho cancel.
                if (order.OrderStatus != OrderStatus.Pending
                    && order.OrderStatus != OrderStatus.Processing)
                {
                    await _uow.RollbackTransactionAsync();
                    return new CancelOrderResult
                    {
                        Success = false,
                        Message = $"Không thể hủy đơn ở trạng thái '{order.OrderStatus}'. Vui lòng liên hệ CSKH nếu đã thanh toán."
                    };
                }

                // Hoàn ReservedQuantity cho từng OrderDetail
                foreach (var detail in order.OrderDetails)
                {
                    var product = await _uow.Products.GetByIdAsync(detail.ProductId);
                    if (product != null)
                    {
                        product.ReservedQuantity = Math.Max(0, product.ReservedQuantity - detail.Quantity);
                        _uow.Products.Update(product);
                    }
                }

                // Hoàn voucher (nếu có)
                await _voucherService.RestoreAsync(order.OrderId);

                // ✅ Nếu payment đã thành công (rất hiếm vì chỉ Pending/Processing) → refund
                if (order.Payment?.TransactionStatus == PaymentStatus.Completed)
                {
                    await _paymentService.RefundAsync(order.Payment.PaymentId, "Customer cancel order");
                }

                // Set Cancelled
                order.OrderStatus = OrderStatus.Cancelled;
                order.UpdatedAt = DateTime.UtcNow;
                _uow.Orders.Update(order);

                await _uow.CommitTransactionAsync();

                await _auditLogService.LogAsync(
                    userId, null,
                    AuditAction.OrderCancelled,
                    description: $"Order cancelled: {order.OrderCode}",
                    isSuccess: true,
                    controller: "Order",
                    actionName: "Cancel"
                );

                return new CancelOrderResult { Success = true, Message = "Đã hủy đơn hàng." };
            }
            catch (Exception ex)
            {
                await _uow.RollbackTransactionAsync();
                Log.Error(ex, "CancelOrder failed: orderId={OrderId}, userId={UserId}", orderId, userId);
                return new CancelOrderResult { Success = false, Message = "Hủy đơn thất bại. Vui lòng thử lại." };
            }
        }

        // ============================================================
        // Queries
        // ============================================================
        public async Task<PagedResult<Order>> GetUserOrdersAsync(int userId, int pageNumber, int pageSize)
        {
            return await _uow.Orders.GetByUserAsync(userId, pageNumber, pageSize);
        }

        public async Task<Order?> GetOrderDetailAsync(int orderId)
        {
            return await _uow.Orders.GetWithDetailsAsync(orderId);
        }

        public async Task<Order?> GetOrderByCodeAsync(string code)
        {
            return await _uow.Orders.GetByCodeAsync(code);
        }

        // ============================================================
        // Phase 4 - Staff operations
        // ============================================================
        public async Task<PagedResult<Order>> GetPagedAsync(int pageNumber, int pageSize, OrderStatus? status = null, string? search = null)
        {
            return await _uow.Orders.GetPagedAsync(pageNumber, pageSize, status, search);
        }

        public async Task<Order?> GetForStaffAsync(int orderId)
        {
            return await _uow.Orders.GetWithAllAsync(orderId);
        }

        public async Task<OrderActionResult> ConfirmOrderAsync(int orderId, int staffId)
        {
            try
            {
                await _uow.BeginTransactionAsync();

                var order = await _uow.Orders.GetWithDetailsAsync(orderId);
                if (order == null)
                {
                    await _uow.RollbackTransactionAsync();
                    return new OrderActionResult { Success = false, Message = "Không tìm thấy đơn hàng." };
                }

                if (order.OrderStatus != OrderStatus.Pending && order.OrderStatus != OrderStatus.Confirmed)
                {
                    await _uow.RollbackTransactionAsync();
                    return new OrderActionResult
                    {
                        Success = false,
                        Message = $"Không thể xác nhận đơn ở trạng thái '{order.OrderStatus}'."
                    };
                }

                order.OrderStatus = OrderStatus.Confirmed;
                order.UpdatedAt = DateTime.UtcNow;
                _uow.Orders.Update(order);

                await _uow.CommitTransactionAsync();

                await _auditLogService.LogAsync(
                    staffId, null,
                    AuditAction.OrderConfirmed,
                    description: $"Order confirmed: {order.OrderCode}",
                    isSuccess: true,
                    controller: "Staff/Order",
                    actionName: "Confirm"
                );

                return new OrderActionResult { Success = true, Message = "Đã xác nhận đơn.", Order = order };
            }
            catch (Exception ex)
            {
                await _uow.RollbackTransactionAsync();
                Log.Error(ex, "ConfirmOrder failed: orderId={OrderId}", orderId);
                return new OrderActionResult { Success = false, Message = "Có lỗi xảy ra." };
            }
        }

        // ============================================================
        // Helpers: fire-and-forget cho Notification/Email
        // ============================================================
        private static async Task SafeNotifyAsync(Func<Task> action, string context)
        {
            try { await action(); }
            catch (Exception ex) { Log.Error(ex, "[Notification fire-and-forget] {Context}", context); }
        }

        private static async Task SafeEmailAsync(Func<Task> action, string context)
        {
            try { await action(); }
            catch (Exception ex) { Log.Error(ex, "[Email fire-and-forget] {Context}", context); }
        }
    }
}