using System;
using OSBIS.Models.Enums;

namespace OSBIS.Models.Entities
{
    /// <summary>
    /// Lịch sử trạng thái shipment (timeline cho khách hàng tracking đơn).
    /// </summary>
    public class ShipmentTracking
    {
        public int TrackingId { get; set; }
        public int ShipmentId { get; set; }
        public ShipmentStatus Status { get; set; }
        public string? Location { get; set; }
        public string? Note { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public Shipment Shipment { get; set; } = null!;
        public User? UpdatedByUser { get; set; }
    }
}
