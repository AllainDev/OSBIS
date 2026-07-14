using Microsoft.EntityFrameworkCore;
using OSBIS.Data;
using OSBIS.Models.Entities;
using OSBIS.Repositories.Interfaces;

namespace OSBIS.Repositories.Implementations
{
    public class InventoryBatchRepository : IInventoryBatchRepository
    {
        private readonly AppDbContext _context;
        private readonly DbSet<InventoryBatch> _dbSet;

        public InventoryBatchRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.InventoryBatches;
        }

        public async Task<InventoryBatch?> GetByIdAsync(int id)
        {
            return await _dbSet.FirstOrDefaultAsync(b => b.BatchId == id);
        }

        public async Task<InventoryBatch?> GetByBatchCodeAsync(string batchCode)
        {
            return await _dbSet.FirstOrDefaultAsync(b => b.BatchCode == batchCode);
        }

        public async Task<IReadOnlyList<InventoryBatch>> GetByProductAsync(int productId)
        {
            return await _dbSet.AsNoTracking()
                .Where(b => b.ProductId == productId)
                .OrderBy(b => b.ExpiryDate)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<InventoryBatch>> GetExpiringSoonAsync(int withinDays = 30)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var cutoff = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(withinDays));
            return await _dbSet.AsNoTracking()
                .Include(b => b.Product)
                .Where(b => b.ExpiryDate >= today && b.ExpiryDate <= cutoff)
                .OrderBy(b => b.ExpiryDate)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalCostByProductAsync(int productId)
        {
            return await _dbSet.AsNoTracking()
                .Where(b => b.ProductId == productId)
                .SumAsync(b => b.CostPrice * b.Quantity);
        }

        public void Add(InventoryBatch batch)
        {
            _dbSet.Add(batch);
        }

        public void Update(InventoryBatch batch)
        {
            _dbSet.Update(batch);
        }

        public void Remove(InventoryBatch batch)
        {
            _dbSet.Remove(batch);
        }
    }
}
