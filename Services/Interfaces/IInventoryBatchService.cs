using OSBIS.Models.Entities;

namespace OSBIS.Services.Interfaces
{
    /// <summary>
    /// InventoryBatch Service — Phase 2: quản lý lô + cập nhật TotalStock.
    /// </summary>
    public interface IInventoryBatchService
    {
        Task<IReadOnlyList<InventoryBatch>> GetByProductAsync(int productId);
        Task<IReadOnlyList<InventoryBatch>> GetExpiringSoonAsync(int withinDays = 30);

        /// <summary>Thêm lô mới + tự động cộng Quantity vào Product.TotalStock.</summary>
        Task<int> AddBatchAsync(InventoryBatch batch);

        Task<bool> UpdateBatchAsync(InventoryBatch batch);
        Task<bool> DeleteBatchAsync(int id); // trừ ngược lại Product.TotalStock
    }
}
