using System;
using System.Collections.Generic;
using OSBIS.Models.Enums;

namespace OSBIS.Models.Entities
{
    public class Shipment
    {
        public int ShipmentId { get; set; }
        public int OrderId { get; set; }
        public string LogisticsProvider { get; set; } = null!;
        public string? TrackingNumber { get; set; }
        public decimal TotalWeight { get; set; }
        public DateTime? EstimatedDeliveryDate { get; set; }
        public ShipmentStatus ShipmentStatus { get; set; }

        /// <summary>ID shipper nội bộ được phân công (Phase 4)</summary>
        public int? AssignedShipperId { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public Order Order { get; set; } = null!;
        public User? AssignedShipper { get; set; }
        public ICollection<ShipmentTracking> ShipmentTrackings { get; set; } = new List<ShipmentTracking>();
    }
}
