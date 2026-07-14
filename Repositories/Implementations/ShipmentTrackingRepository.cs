using Microsoft.EntityFrameworkCore;
using OSBIS.Data;
using OSBIS.Models.Entities;
using OSBIS.Repositories.Interfaces;

namespace OSBIS.Repositories.Implementations
{
    public class ShipmentTrackingRepository : IShipmentTrackingRepository
    {
        private readonly AppDbContext _context;
        private readonly DbSet<ShipmentTracking> _dbSet;

        public ShipmentTrackingRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.ShipmentTrackings;
        }

        public async Task<IReadOnlyList<ShipmentTracking>> GetByShipmentIdAsync(int shipmentId)
        {
            return await _dbSet
                .Include(t => t.UpdatedByUser)
                .Where(t => t.ShipmentId == shipmentId)
                .OrderByDescending(t => t.UpdatedAt)
                .ToListAsync();
        }

        public async Task AddAsync(ShipmentTracking tracking)
        {
            await _dbSet.AddAsync(tracking);
        }

        public void Update(ShipmentTracking tracking)
        {
            tracking.UpdatedAt = DateTime.UtcNow;
            _dbSet.Update(tracking);
        }

        public void Remove(ShipmentTracking tracking)
        {
            _dbSet.Remove(tracking);
        }

        public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}
