using Microsoft.EntityFrameworkCore;
using OSBIS.Common;
using OSBIS.Data;
using OSBIS.Models.Entities;
using OSBIS.Repositories.Interfaces;

namespace OSBIS.Repositories.Implementations
{
    public class VoucherRepository : IVoucherRepository
    {
        private readonly AppDbContext _context;
        private readonly DbSet<Voucher> _dbSet;

        public VoucherRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Vouchers;
        }

        public async Task<Voucher?> GetByIdAsync(int id)
        {
            return await _dbSet.FirstOrDefaultAsync(v => v.VoucherId == id);
        }

        public async Task<Voucher?> GetByCodeAsync(string code)
        {
            return await _dbSet.FirstOrDefaultAsync(v => v.VoucherCode == code);
        }

        public async Task<PagedResult<Voucher>> GetPagedAsync(int pageNumber, int pageSize)
        {
            var query = _dbSet.AsNoTracking();
            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(v => v.StartDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Voucher>
            {
                Items = items,
                TotalCount = total,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<IReadOnlyList<Voucher>> GetActiveVouchersAsync()
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .Where(v => v.IsActive == true && v.StartDate <= now && v.EndDate >= now)
                .ToListAsync();
        }

        public async Task AddAsync(Voucher voucher)
        {
            await _dbSet.AddAsync(voucher);
            // KHÔNG SaveChanges — UnitOfWork commit
        }

        public void Update(Voucher voucher)
        {
            _dbSet.Update(voucher);
        }

        public void Remove(Voucher voucher)
        {
            _dbSet.Remove(voucher);
        }

        public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}
