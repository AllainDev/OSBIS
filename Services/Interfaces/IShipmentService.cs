using OSBIS.Common;
using OSBIS.Models.Entities;
using OSBIS.Models.Enums;

namespace OSBIS.Services.Interfaces
{
    /// <summary>
    /// Service quản lý Shipment (Phase 4).
    /// </summary>
    public interface IShipmentService
    {
        Task<Shipment?> GetByIdAsync(int shipmentId);
        Task<Shipment?> GetWithTrackingsAsync(int shipmentId);
        Task<Shipment?> GetByOrderIdAsync(int orderId);
        Task<IReadOnlyList<ShipmentTracking>> GetTrackingHistoryAsync(int shipmentId);

        Task<PagedResult<Shipment>> GetPagedAsync(int pageNumber, int pageSize, ShipmentStatus? status = null);
        Task<IReadOnlyList<Shipment>> GetByShipperAsync(int shipperId, ShipmentStatus? status = null);

        /// <summary>Staff tạo shipment cho order (sau khi payment success). Tạo Shipment + ShipmentTracking đầu tiên.</summary>
        Task<ShipmentResult> CreateShipmentAsync(int orderId, int? shipperId, int staffId);

        /// <summary>Staff/Shipper update shipment status (PickedUp, InTransit, Delivered, Failed). Mapping sang OrderStatus.</summary>
        Task<ShipmentResult> UpdateStatusAsync(int shipmentId, ShipmentStatus newStatus, string? location, string? note, int updatedBy);

        /// <summary>Shipper xác nhận đã thu tiền COD.</summary>
        Task<ShipmentResult> ConfirmCODReceivedAsync(int shipmentId, int shipperId);

        /// <summary>Staff gán shipper cho shipment.</summary>
        Task<ShipmentResult> AssignShipperAsync(int shipmentId, int shipperId, int staffId);
    }

    public class ShipmentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Shipment? Shipment { get; set; }
    }
}
