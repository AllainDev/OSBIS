using System;
using System.Collections.Generic;
using OSBIS.Models.Enums;

namespace OSBIS.Models.Entities
{
    public class Order
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public int? VoucherId { get; set; }

        /// <summary>Mã đơn hàng format OSB-yyyyMMdd-#### (unique, do OrderService.PlaceOrderAsync sinh ra)</summary>
        public string OrderCode { get; set; } = null!;

        public DateTime? OrderDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string ShippingAddress { get; set; } = null!;
        public OrderStatus OrderStatus { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public User User { get; set; } = null!;
        public Voucher? Voucher { get; set; }
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public Payment? Payment { get; set; }
        public Shipment? Shipment { get; set; }
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
