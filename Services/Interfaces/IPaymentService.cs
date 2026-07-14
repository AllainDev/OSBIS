using OSBIS.Models.Entities;
using OSBIS.Models.Enums;

namespace OSBIS.Services.Interfaces
{
    /// <summary>
    /// Service quản lý Payment.
    /// Phase 3: tạo Payment ở trạng thái Pending.
    /// Phase 4: bổ sung ConfirmBankTransferAsync, ConfirmCODReceivedAsync, RefundAsync.
    /// </summary>
    public interface IPaymentService
    {
        /// <summary>Tạo object Payment (Pending) — KHÔNG save, để caller add vào DbSet + commit trong transaction.</summary>
        Payment CreatePaymentEntity(int orderId, int paymentMethod, decimal amount, string? billImageUrl = null);

        /// <summary>Tạo và lưu Payment (dùng ngoài transaction).</summary>
        Task<Payment> CreatePaymentAsync(int orderId, int paymentMethod, decimal amount, string? billImageUrl = null);

        // ====== Phase 4: Confirm & Refund ======

        /// <summary>Staff xác nhận đã nhận CK → Payment.Status = Success, Order.Status = Processing (nếu Pending).</summary>
        Task<PaymentResult> ConfirmBankTransferAsync(int paymentId, string billImageUrl, int staffId);

        /// <summary>Shipper xác nhận đã thu tiền COD → Payment.Status = Success, Order.Status = Completed (nếu đã Delivered).</summary>
        Task<PaymentResult> ConfirmCODReceivedAsync(int paymentId, int shipperId);

        /// <summary>Refund payment khi order bị cancel sau khi đã thanh toán.</summary>
        Task<PaymentResult> RefundAsync(int paymentId, string reason);

        /// <summary>Lấy Payment theo OrderId.</summary>
        Task<Payment?> GetByOrderIdAsync(int orderId);
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Payment? Payment { get; set; }
    }
}
