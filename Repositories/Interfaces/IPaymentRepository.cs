using OSBIS.Common;
using OSBIS.Models.Entities;
using OSBIS.Models.Enums;

namespace OSBIS.Repositories.Interfaces
{
    /// <summary>
    /// Repository cho Payment (1-1 với Order). Phase 4 mở rộng.
    /// </summary>
    public interface IPaymentRepository
    {
        Task<Payment?> GetByOrderIdAsync(int orderId);
        Task<Payment?> GetByIdAsync(int paymentId);

        Task<IReadOnlyList<Payment>> GetByStatusAsync(PaymentStatus status);

        /// <summary>Lấy tất cả payment có phân trang (dùng cho Admin/Staff).</summary>
        Task<PagedResult<Payment>> GetPagedAsync(int pageNumber, int pageSize, PaymentStatus? status = null);

        /// <summary>Add mới Payment vào DbSet (chưa save).</summary>
        Task AddAsync(Payment payment);

        /// <summary>Update Payment (chưa save).</summary>
        void Update(Payment payment);

        Task<int> SaveChangesAsync();
    }
}
