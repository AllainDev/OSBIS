using System;
using System.Collections.Generic;
using OSBIS.Models.Enums;

namespace OSBIS.Models.Entities
{
    public class Voucher
    {
        public int VoucherId { get; set; }
        public string VoucherCode { get; set; } = null!;
        public DiscountType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal? MinOrderValue { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int UsageLimit { get; set; }
        public int? UsedCount { get; set; }
        public bool? IsActive { get; set; }

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}