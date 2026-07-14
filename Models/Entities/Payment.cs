using System;
using OSBIS.Models.Enums;

namespace OSBIS.Models.Entities
{
    public class Payment
    {
        public int PaymentId { get; set; }
        public int OrderId { get; set; }
        public string? ProviderTransactionId { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public DateTime? PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus TransactionStatus { get; set; }
        public string? BillImageUrl { get; set; } // Ảnh chứng từ CK (Phase 4 sẽ dùng)
        public DateTime? UpdatedAt { get; set; }

        public Order Order { get; set; } = null!;
    }
}
