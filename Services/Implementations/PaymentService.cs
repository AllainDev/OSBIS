using OSBIS.Models.Entities;
using OSBIS.Models.Enums;
using OSBIS.Repositories.Interfaces;
using OSBIS.Services.Interfaces;
using Serilog;

namespace OSBIS.Services.Implementations
{
    /// <summary>
    /// Service quản lý Payment.
    /// Phase 3: Create (Pending).
    /// Phase 4: ConfirmBankTransfer / ConfirmCOD / Refund + trừ TotalStock khi thành công.
    /// Phase 5: tích hợp IEmailService (NotificationService gọi riêng từ OrderService/ShipmentService).
    ///
    /// FIX đã áp dụng:
    /// - ConfirmBankTransferAsync: gọi SendPaymentConfirmationAsync sau commit
    /// - ConfirmCODReceivedAsync: gọi SendPaymentConfirmationAsync sau commit
    /// </summary>
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _uow;
        private readonly IAuditLogService _auditLogService;
        private readonly IEmailService _emailService;

        public PaymentService(
            IUnitOfWork uow,
            IAuditLogService auditLogService,
            IEmailService emailService)
        {
            _uow = uow;
            _auditLogService = auditLogService;
            _emailService = emailService;
        }

        // ============================================================
        // Phase 3
        // ============================================================
        public Payment CreatePaymentEntity(int orderId, int paymentMethod, decimal amount, string? billImageUrl = null)
        {
            return new Payment
            {
                OrderId = orderId,
                PaymentMethod = (PaymentMethod)paymentMethod,
                Amount = amount,
                TransactionStatus = PaymentStatus.Pending,
                PaymentDate = DateTime.UtcNow,
                BillImageUrl = billImageUrl,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public async Task<Payment> CreatePaymentAsync(int orderId, int paymentMethod, decimal amount, string? billImageUrl = null)
        {
            var payment = CreatePaymentEntity(orderId, paymentMethod, amount, billImageUrl);
            await _uow.Payments.AddAsync(payment);
            return payment;
        }

        public async Task<Payment?> GetByOrderIdAsync(int orderId)
        {
            return await _uow.Payments.GetByOrderIdAsync(orderId);
        }

        // ============================================================
        // Phase 4 — Confirm BankTransfer
        // ============================================================
        public async Task<PaymentResult> ConfirmBankTransferAsync(int paymentId, string billImageUrl, int staffId)
        {
            Payment? payment = null;
            Order? order = null;

            try
            {
                await _uow.BeginTransactionAsync();

                payment = await _uow.Payments.GetByIdAsync(paymentId);
                if (payment == null)
                {
                    await _uow.RollbackTransactionAsync();
                    return new PaymentResult { Success = false, Message = "Không tìm thấy payment." };
                }

                if (payment.TransactionStatus == PaymentStatus.Completed)
                {
                    await _uow.RollbackTransactionAsync();
                    return new PaymentResult { Success = false, Message = "Payment đã được xác nhận trước đó." };
                }

                if (payment.PaymentMethod != PaymentMethod.BankTransfer)
                {
                    await _uow.RollbackTransactionAsync();
                    return new PaymentResult { Success = false, Message = "Payment này không phải chuyển khoản." };
                }

                // Cập nhật payment
                payment.TransactionStatus = PaymentStatus.Completed;
                payment.BillImageUrl = billImageUrl;
                payment.ProviderTransactionId = $"BT-{DateTime.UtcNow:yyyyMMddHHmmss}";
                payment.UpdatedAt = DateTime.UtcNow;
                _uow.Payments.Update(payment);

                // Cập nhật order: Pending → Confirmed
                order = await _uow.Orders.GetWithDetailsAsync(payment.OrderId);
                if (order != null && order.OrderStatus == OrderStatus.Pending)
                {
                    order.OrderStatus = OrderStatus.Confirmed;
                    _uow.Orders.Update(order);
                }

                // Trừ stock (chuyển từ reserved sang thực sự trừ TotalStock)
                await DeductStockForOrderAsync(order);

                await _uow.CommitTransactionAsync();

                await _auditLogService.LogAsync(
                    staffId, null,
                    AuditAction.PaymentConfirmed,
                    description: $"BankTransfer confirmed: order {order?.OrderCode} - {payment.Amount:N0}đ",
                    isSuccess: true,
                    controller: "Staff/Order",
                    actionName: "ConfirmPayment"
                );

                // ✅ FIX: Gửi email xác nhận thanh toán (fire-and-forget)
                _ = SafeEmailAsync(() => _emailService.SendPaymentConfirmationAsync(payment),
                    $"Email payment confirmation for order {order?.OrderCode}");

                return new PaymentResult { Success = true, Message = "Xác nhận thanh toán thành công.", Payment = payment };
            }
            catch (Exception ex)
            {
                await _uow.RollbackTransactionAsync();
                Log.Error(ex, "ConfirmBankTransfer failed: paymentId={PaymentId}", paymentId);
                return new PaymentResult { Success = false, Message = "Có lỗi xảy ra. Vui lòng thử lại." };
            }
        }

        // ============================================================
        // Phase 4 — Confirm COD
        // ============================================================
        public async Task<PaymentResult> ConfirmCODReceivedAsync(int paymentId, int shipperId)
        {
            Payment? payment = null;
            Order? order = null;

            try
            {
                await _uow.BeginTransactionAsync();

                payment = await _uow.Payments.GetByIdAsync(paymentId);
                if (payment == null)
                {
                    await _uow.RollbackTransactionAsync();
                    return new PaymentResult { Success = false, Message = "Không tìm thấy payment." };
                }

                if (payment.TransactionStatus == PaymentStatus.Completed)
                {
                    await _uow.RollbackTransactionAsync();
                    return new PaymentResult { Success = false, Message = "Payment đã được xác nhận." };
                }

                if (payment.PaymentMethod != PaymentMethod.COD)
                {
                    await _uow.RollbackTransactionAsync();
                    return new PaymentResult { Success = false, Message = "Payment này không phải COD." };
                }

                payment.TransactionStatus = PaymentStatus.Completed;
                payment.ProviderTransactionId = $"COD-{DateTime.UtcNow:yyyyMMddHHmmss}";
                payment.UpdatedAt = DateTime.UtcNow;
                _uow.Payments.Update(payment);

                // Order: nếu đã Delivered → Completed
                order = await _uow.Orders.GetWithDetailsAsync(payment.OrderId);
                if (order != null && order.OrderStatus == OrderStatus.Delivered)
                {
                    order.OrderStatus = OrderStatus.Completed;
                    order.UpdatedAt = DateTime.UtcNow;
                    _uow.Orders.Update(order);
                }

                // Trừ stock khi COD cũng thành công
                await DeductStockForOrderAsync(order);

                await _uow.CommitTransactionAsync();

                await _auditLogService.LogAsync(
                    shipperId, null,
                    AuditAction.PaymentConfirmed,
                    description: $"COD received: order {order?.OrderCode}",
                    isSuccess: true,
                    controller: "Shipper/Shipment",
                    actionName: "ConfirmCOD"
                );

                // ✅ FIX: Gửi email xác nhận COD
                _ = SafeEmailAsync(() => _emailService.SendPaymentConfirmationAsync(payment),
                    $"Email COD confirmation for order {order?.OrderCode}");

                return new PaymentResult { Success = true, Message = "Xác nhận thu COD thành công.", Payment = payment };
            }
            catch (Exception ex)
            {
                await _uow.RollbackTransactionAsync();
                Log.Error(ex, "ConfirmCODReceived failed: paymentId={PaymentId}", paymentId);
                return new PaymentResult { Success = false, Message = "Có lỗi xảy ra. Vui lòng thử lại." };
            }
        }

        // ============================================================
        // Phase 4 — Refund
        // ============================================================
        public async Task<PaymentResult> RefundAsync(int paymentId, string reason)
        {
            try
            {
                await _uow.BeginTransactionAsync();

                var payment = await _uow.Payments.GetByIdAsync(paymentId);
                if (payment == null)
                {
                    await _uow.RollbackTransactionAsync();
                    return new PaymentResult { Success = false, Message = "Không tìm thấy payment." };
                }

                if (payment.TransactionStatus != PaymentStatus.Completed)
                {
                    await _uow.RollbackTransactionAsync();
                    return new PaymentResult { Success = false, Message = "Chỉ hoàn tiền cho payment đã thanh toán." };
                }

                payment.TransactionStatus = PaymentStatus.Refunded;
                payment.UpdatedAt = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(reason))
                    payment.ProviderTransactionId = $"REFUND-{DateTime.UtcNow:yyyyMMddHHmmss} | {reason}";
                _uow.Payments.Update(payment);

                // Order status chuyển sang Refunded
                var order = await _uow.Orders.GetByIdAsync(payment.OrderId);
                if (order != null)
                {
                    order.OrderStatus = OrderStatus.Refunded;
                    order.UpdatedAt = DateTime.UtcNow;
                    _uow.Orders.Update(order);
                }

                await _uow.CommitTransactionAsync();

                await _auditLogService.LogAsync(
                    null, "system",
                    AuditAction.PaymentRefunded,
                    description: $"Payment refunded: order {order?.OrderCode} - {payment.Amount:N0}đ | Reason: {reason}",
                    isSuccess: true,
                    controller: "Order",
                    actionName: "Refund"
                );

                return new PaymentResult { Success = true, Message = "Hoàn tiền thành công.", Payment = payment };
            }
            catch (Exception ex)
            {
                await _uow.RollbackTransactionAsync();
                Log.Error(ex, "Refund failed: paymentId={PaymentId}", paymentId);
                return new PaymentResult { Success = false, Message = "Có lỗi xảy ra khi hoàn tiền." };
            }
        }

        // ============================================================
        // Helper: trừ TotalStock khi payment success
        // ============================================================
        private async Task DeductStockForOrderAsync(Order? order)
        {
            if (order == null) return;

            foreach (var detail in order.OrderDetails)
            {
                var product = await _uow.Products.GetByIdAsync(detail.ProductId);
                if (product == null) continue;

                // TotalStock -= qty, ReservedQuantity -= qty (chuyển từ reserved sang thực sự bán)
                product.TotalStock = Math.Max(0, product.TotalStock - detail.Quantity);
                product.ReservedQuantity = Math.Max(0, product.ReservedQuantity - detail.Quantity);
                _uow.Products.Update(product);
            }
        }

        private static async Task SafeEmailAsync(Func<Task> action, string context)
        {
            try { await action(); }
            catch (Exception ex) { Log.Error(ex, "[Email fire-and-forget] {Context}", context); }
        }
    }
}