using System;

namespace ORBIS.Models.Entities
{
    public class Shipment
    {
        public int ShipmentId { get; set; }
        public int OrderId { get; set; }
        public string LogisticsProvider { get; set; } = null!;
        public string? TrackingNumber { get; set; }
        public decimal TotalWeight { get; set; }
        public DateTime? EstimatedDeliveryDate { get; set; }
        public byte ShipmentStatus { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public Order Order { get; set; } = null!;
    }
}
