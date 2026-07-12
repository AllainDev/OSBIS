using System;
using System.Collections.Generic;

namespace ORBIS.Models.Entities
{
    public class Order
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public int? VoucherId { get; set; }
        public DateTime? OrderDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string ShippingAddress { get; set; } = null!;
        public byte OrderStatus { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public User User { get; set; } = null!;
        public Voucher? Voucher { get; set; }
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public Payment? Payment { get; set; }
        public Shipment? Shipment { get; set; }
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
