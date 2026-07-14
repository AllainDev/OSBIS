using Microsoft.EntityFrameworkCore;
using OSBIS.Data;
using OSBIS.Models.Entities;
using OSBIS.Repositories.Interfaces;

namespace OSBIS.Repositories.Implementations
{
    public class VoucherUsageRepository : IVoucherUsageRepository
    {
        private readonly AppDbContext _context;
        private readonly DbSet<VoucherUsage> _dbSet;

        public VoucherUsageRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.VoucherUsages;
        }

        public async Task<VoucherUsage?> GetByIdAsync(int id)
        {
            return await _dbSet.FirstOrDefaultAsync(vu => vu.VoucherUsageId == id);
        }

        public async Task<VoucherUsage?> GetByOrderIdAsync(int orderId)
        {
            return await _dbSet.FirstOrDefaultAsync(vu => vu.OrderId == orderId);
        }

        public async Task<IReadOnlyList<VoucherUsage>> GetByVoucherIdAsync(int voucherId)
        {
            return await _dbSet.AsNoTracking()
                .Include(vu => vu.User)
                .Include(vu => vu.Order)
                .Where(vu => vu.VoucherId == voucherId)
                .OrderByDescending(vu => vu.UsedAt)
                .ToListAsync();
        }

        public async Task<bool> HasUserUsedVoucherAsync(int userId, int voucherId)
        {
            return await _dbSet.AnyAsync(vu => vu.UserId == userId && vu.VoucherId == voucherId);
        }

        public async Task AddAsync(VoucherUsage usage)
        {
            await _dbSet.AddAsync(usage);
        }

        public void Update(VoucherUsage usage)
        {
            _dbSet.Update(usage);
        }

        public void Remove(VoucherUsage usage)
        {
            _dbSet.Remove(usage);
        }

        public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}
