using Microsoft.EntityFrameworkCore;
using OSBIS.Data;
using OSBIS.Models.Entities;
using OSBIS.Repositories.Interfaces;

namespace OSBIS.Repositories.Implementations
{
    public class OrderDetailRepository : IOrderDetailRepository
    {
        private readonly AppDbContext _context;
        private readonly DbSet<OrderDetail> _dbSet;

        public OrderDetailRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.OrderDetails;
        }

        public async Task AddAsync(OrderDetail detail)
        {
            await _dbSet.AddAsync(detail);
            await _context.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<OrderDetail>> GetByOrderIdAsync(int orderId)
        {
            return await _dbSet
                .Where(d => d.OrderId == orderId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<OrderDetail?> GetAsync(int orderId, int productId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(d => d.OrderId == orderId && d.ProductId == productId);
        }
    }
}
