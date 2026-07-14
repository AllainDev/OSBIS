using OSBIS.Models.Entities;

namespace OSBIS.Repositories.Interfaces
{
    /// <summary>
    /// Repository cho InventoryBatch — quản lý lô hàng theo HSD.
    /// </summary>
    public interface IInventoryBatchRepository
    {
        Task<InventoryBatch?> GetByIdAsync(int id);
        Task<InventoryBatch?> GetByBatchCodeAsync(string batchCode);
        Task<IReadOnlyList<InventoryBatch>> GetByProductAsync(int productId);
        Task<IReadOnlyList<InventoryBatch>> GetExpiringSoonAsync(int withinDays = 30);
        Task<decimal> GetTotalCostByProductAsync(int productId);

        void Add(InventoryBatch batch);
        void Update(InventoryBatch batch);
        void Remove(InventoryBatch batch);
    }
}
