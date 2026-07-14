using Microsoft.EntityFrameworkCore;
using OSBIS.Common;
using OSBIS.Data;
using OSBIS.Models.Entities;
using OSBIS.Models.Enums;
using OSBIS.Repositories.Interfaces;

namespace OSBIS.Repositories.Implementations
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly AppDbContext _context;
        private readonly DbSet<Payment> _dbSet;

        public PaymentRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Payments;
        }

        public async Task<Payment?> GetByOrderIdAsync(int orderId)
        {
            return await _dbSet.FirstOrDefaultAsync(p => p.OrderId == orderId);
        }

        public async Task<Payment?> GetByIdAsync(int paymentId)
        {
            return await _dbSet.FirstOrDefaultAsync(p => p.PaymentId == paymentId);
        }

        public async Task<IReadOnlyList<Payment>> GetByStatusAsync(PaymentStatus status)
        {
            return await _dbSet.AsNoTracking()
                .Where(p => p.TransactionStatus == status)
                .ToListAsync();
        }

        public async Task<PagedResult<Payment>> GetPagedAsync(int pageNumber, int pageSize, PaymentStatus? status = null)
        {
            var query = _dbSet.AsNoTracking().Include(p => p.Order).AsQueryable();
            if (status.HasValue)
                query = query.Where(p => p.TransactionStatus == status.Value);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(p => p.PaymentDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Payment>
            {
                Items = items,
                TotalCount = total,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task AddAsync(Payment payment)
        {
            await _dbSet.AddAsync(payment);
            // KHÔNG SaveChanges — để UnitOfWork commit transaction
        }

        public void Update(Payment payment)
        {
            payment.UpdatedAt = DateTime.UtcNow;
            _dbSet.Update(payment);
            // KHÔNG SaveChanges — để UnitOfWork commit transaction
        }

        public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}
