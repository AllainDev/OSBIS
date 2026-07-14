using OSBIS.Models.Entities;

namespace OSBIS.Repositories.Interfaces
{
    /// <summary>
    /// Repository cho OrderDetail. OrderDetail không có IDENTITY (composite key OrderId+ProductId),
    /// nên dùng add-only, update qua _uow.Entry.
    /// </summary>
    public interface IOrderDetailRepository
    {
        Task AddAsync(OrderDetail detail);
        Task<IReadOnlyList<OrderDetail>> GetByOrderIdAsync(int orderId);
        Task<OrderDetail?> GetAsync(int orderId, int productId);
    }
}
