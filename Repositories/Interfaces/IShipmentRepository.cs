using OSBIS.Common;
using OSBIS.Models.Entities;
using OSBIS.Models.Enums;

namespace OSBIS.Repositories.Interfaces
{
    /// <summary>Repository cho Shipment (Phase 4).</summary>
    public interface IShipmentRepository
    {
        Task<Shipment?> GetByIdAsync(int id);
        Task<Shipment?> GetByOrderIdAsync(int orderId);
        Task<Shipment?> GetWithTrackingsAsync(int shipmentId);

        Task<IReadOnlyList<Shipment>> GetByShipperAsync(int shipperId, ShipmentStatus? status = null);
        Task<IReadOnlyList<Shipment>> GetByStatusAsync(ShipmentStatus status);
        Task<PagedResult<Shipment>> GetPagedAsync(int pageNumber, int pageSize, ShipmentStatus? status = null);

        Task AddAsync(Shipment shipment);
        void Update(Shipment shipment);

        Task<int> SaveChangesAsync();
    }
}
