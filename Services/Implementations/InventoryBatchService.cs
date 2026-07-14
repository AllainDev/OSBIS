using OSBIS.Models.Entities;
using OSBIS.Repositories.Interfaces;
using OSBIS.Services.Interfaces;

namespace OSBIS.Services.Implementations
{
    public class InventoryBatchService : IInventoryBatchService
    {
        private readonly IUnitOfWork _uow;

        public InventoryBatchService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<IReadOnlyList<InventoryBatch>> GetByProductAsync(int productId)
            => await _uow.InventoryBatches.GetByProductAsync(productId);

        public async Task<IReadOnlyList<InventoryBatch>> GetExpiringSoonAsync(int withinDays = 30)
            => await _uow.InventoryBatches.GetExpiringSoonAsync(withinDays);

        public async Task<int> AddBatchAsync(InventoryBatch batch)
        {
            await _uow.BeginTransactionAsync();
            try
            {
                _uow.InventoryBatches.Add(batch);

                // Cộng TotalStock cho Product
                var product = await _uow.Products.GetByIdAsync(batch.ProductId);
                if (product == null)
                    throw new InvalidOperationException("Sản phẩm không tồn tại.");

                product.TotalStock += batch.Quantity;
                product.UpdatedAt = DateTime.UtcNow;
                _uow.Products.Update(product);

                await _uow.CommitTransactionAsync();
                return batch.BatchId;
            }
            catch
            {
                await _uow.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<bool> UpdateBatchAsync(InventoryBatch batch)
        {
            _uow.InventoryBatches.Update(batch);
            await _uow.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteBatchAsync(int id)
        {
            var batch = await _uow.InventoryBatches.GetByIdAsync(id);
            if (batch == null) return false;

            await _uow.BeginTransactionAsync();
            try
            {
                _uow.InventoryBatches.Remove(batch);

                // Giảm Product.TotalStock
                var product = await _uow.Products.GetByIdAsync(batch.ProductId);
                if (product != null)
                {
                    var newStock = Math.Max(0, product.TotalStock - batch.Quantity);
                    product.TotalStock = newStock;
                    product.UpdatedAt = DateTime.UtcNow;
                    _uow.Products.Update(product);
                }

                await _uow.CommitTransactionAsync();
                return true;
            }
            catch
            {
                await _uow.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
