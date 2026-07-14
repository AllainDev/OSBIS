using Microsoft.EntityFrameworkCore;
using OSBIS.Common;
using OSBIS.Data;
using OSBIS.Models.Entities;
using OSBIS.Models.Enums;
using OSBIS.Repositories.Interfaces;

namespace OSBIS.Repositories.Implementations
{
    public class ShipmentRepository : IShipmentRepository
    {
        private readonly AppDbContext _context;
        private readonly DbSet<Shipment> _dbSet;

        public ShipmentRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Shipments;
        }

        public async Task<Shipment?> GetByIdAsync(int id)
        {
            return await _dbSet.FirstOrDefaultAsync(s => s.ShipmentId == id);
        }

        public async Task<Shipment?> GetByOrderIdAsync(int orderId)
        {
            return await _dbSet.FirstOrDefaultAsync(s => s.OrderId == orderId);
        }

        public async Task<Shipment?> GetWithTrackingsAsync(int shipmentId)
        {
            return await _dbSet
                .Include(s => s.ShipmentTrackings.OrderByDescending(t => t.UpdatedAt))
                .Include(s => s.Order).ThenInclude(o => o!.OrderDetails)
                .Include(s => s.AssignedShipper)
                .FirstOrDefaultAsync(s => s.ShipmentId == shipmentId);
        }

        public async Task<IReadOnlyList<Shipment>> GetByShipperAsync(int shipperId, ShipmentStatus? status = null)
        {
            var query = _dbSet
                .Include(s => s.Order).ThenInclude(o => o!.OrderDetails)
                .Include(s => s.AssignedShipper)
                .Where(s => s.AssignedShipperId == shipperId);

            if (status.HasValue)
                query = query.Where(s => s.ShipmentStatus == status.Value);

            return await query.OrderByDescending(s => s.UpdatedAt).ToListAsync();
        }

        public async Task<IReadOnlyList<Shipment>> GetByStatusAsync(ShipmentStatus status)
        {
            return await _dbSet.AsNoTracking()
                .Include(s => s.Order)
                .Where(s => s.ShipmentStatus == status)
                .ToListAsync();
        }

        public async Task<PagedResult<Shipment>> GetPagedAsync(int pageNumber, int pageSize, ShipmentStatus? status = null)
        {
            var query = _dbSet.AsNoTracking()
                .Include(s => s.Order)
                .Include(s => s.AssignedShipper)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(s => s.ShipmentStatus == status.Value);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(s => s.UpdatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Shipment>
            {
                Items = items,
                TotalCount = total,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task AddAsync(Shipment shipment)
        {
            await _dbSet.AddAsync(shipment);
        }

        public void Update(Shipment shipment)
        {
            shipment.UpdatedAt = DateTime.UtcNow;
            _dbSet.Update(shipment);
        }

        public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}
