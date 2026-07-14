using OSBIS.Models.Entities;

namespace OSBIS.Services.Interfaces
{
    /// <summary>
    /// Service gửi email (Phase 5).
    /// Phase 3: Phase 5 chưa tích hợp SMTP thực — sẽ log ra console.
    /// </summary>
    public interface IEmailService
    {
        Task SendOrderConfirmationAsync(Order order);
        Task SendShippingUpdateAsync(Order order, string statusNote);
        Task SendPaymentConfirmationAsync(Payment payment);
        Task SendVoucherCodeAsync(User user, Voucher voucher);
    }
}
