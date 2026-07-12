using System;

namespace ORBIS.Models.Entities
{
    public class Payment
    {
        public int PaymentId { get; set; }
        public int OrderId { get; set; }
        public string? ProviderTransactionId { get; set; }
        public byte PaymentMethod { get; set; }
        public DateTime? PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public byte TransactionStatus { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public Order Order { get; set; } = null!;
    }
}
