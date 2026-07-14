using Microsoft.EntityFrameworkCore;
using OSBIS.Common;
using OSBIS.Data;
using OSBIS.Models.Entities;
using OSBIS.Models.Enums;
using OSBIS.Repositories.Interfaces;

namespace OSBIS.Repositories.Implementations
{
    public class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _context;
        private readonly DbSet<Order> _dbSet;

        public OrderRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Orders;
        }

        public async Task<Order?> GetByIdAsync(int orderId)
        {
            return await _dbSet.FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        public async Task<Order?> GetWithDetailsAsync(int orderId)
        {
            return await _dbSet
                .Include(o => o.OrderDetails)
                .Include(o => o.Payment)
                .Include(o => o.Shipment).ThenInclude(s => s!.ShipmentTrackings)
                .Include(o => o.Voucher)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        public async Task<Order?> GetWithAllAsync(int orderId)
        {
            return await _dbSet
                .Include(o => o.OrderDetails)
                .Include(o => o.Payment)
                .Include(o => o.Shipment).ThenInclude(s => s!.ShipmentTrackings).ThenInclude(t => t.UpdatedByUser)
                .Include(o => o.Shipment).ThenInclude(s => s!.AssignedShipper)
                .Include(o => o.Voucher)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        public async Task<Order?> GetByCodeAsync(string orderCode)
        {
            return await _dbSet.FirstOrDefaultAsync(o => o.OrderCode == orderCode);
        }

        public async Task<PagedResult<Order>> GetByUserAsync(int userId, int pageNumber, int pageSize, OrderStatus? status = null)
        {
            var query = _dbSet.AsNoTracking().Where(o => o.UserId == userId);
            if (status.HasValue)
                query = query.Where(o => o.OrderStatus == status.Value);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Order>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<Order>> GetPagedAsync(int pageNumber, int pageSize, OrderStatus? status = null, string? search = null)
        {
            var query = _dbSet.AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.Payment)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(o => o.OrderStatus == status.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(o => o.OrderCode.ToLower().Contains(s)
                                       || (o.User != null && o.User.Email.ToLower().Contains(s))
                                       || (o.User != null && o.User.FullName.ToLower().Contains(s)));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Order>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task AddAsync(Order order)
        {
            await _dbSet.AddAsync(order);
            // KHÔNG SaveChanges — UnitOfWork commit
        }

        public void Update(Order order)
        {
            order.UpdatedAt = DateTime.UtcNow;
            _dbSet.Update(order);
            // KHÔNG SaveChanges — UnitOfWork commit
        }

        public void SoftDelete(Order order)
        {
            _dbSet.Remove(order);
        }

        public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}
