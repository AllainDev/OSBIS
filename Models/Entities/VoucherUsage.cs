using System;

namespace OSBIS.Models.Entities
{
    /// <summary>
    /// Lịch sử dùng voucher — chống abuse, enforce per-user quota.
    /// </summary>
    public class VoucherUsage
    {
        public int VoucherUsageId { get; set; }
        public int VoucherId { get; set; }
        public int UserId { get; set; }
        public int OrderId { get; set; }
        public DateTime? UsedAt { get; set; }

        public Voucher Voucher { get; set; } = null!;
        public User User { get; set; } = null!;
        public Order Order { get; set; } = null!;
    }
}
