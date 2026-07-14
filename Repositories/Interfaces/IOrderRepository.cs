using OSBIS.Common;
using OSBIS.Models.Entities;
using OSBIS.Models.Enums;

namespace OSBIS.Repositories.Interfaces
{
    /// <summary>
    /// Repository cho Order.
    /// </summary>
    public interface IOrderRepository
    {
        Task<Order?> GetByIdAsync(int orderId);
        Task<Order?> GetWithDetailsAsync(int orderId);
        Task<Order?> GetWithAllAsync(int orderId); // Include OrderDetails + Payment + Shipment + Voucher + User
        Task<Order?> GetByCodeAsync(string orderCode);

        Task<PagedResult<Order>> GetByUserAsync(int userId, int pageNumber, int pageSize, OrderStatus? status = null);

        /// <summary>Lấy tất cả đơn (dùng cho Staff/Admin), filter theo status.</summary>
        Task<PagedResult<Order>> GetPagedAsync(int pageNumber, int pageSize, OrderStatus? status = null, string? search = null);

        Task AddAsync(Order order);
        void Update(Order order);
        void SoftDelete(Order order);

        Task<int> SaveChangesAsync();
    }
}
