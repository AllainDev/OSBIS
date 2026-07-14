using OSBIS.Models.Entities;

namespace OSBIS.Repositories.Interfaces
{
    /// <summary>Repository cho ShipmentTracking (Phase 4 - timeline).</summary>
    public interface IShipmentTrackingRepository
    {
        Task<IReadOnlyList<ShipmentTracking>> GetByShipmentIdAsync(int shipmentId);

        Task AddAsync(ShipmentTracking tracking);
        void Update(ShipmentTracking tracking);
        void Remove(ShipmentTracking tracking);

        Task<int> SaveChangesAsync();
    }
}
