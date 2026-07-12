using System;

namespace ORBIS.Models.Entities
{
    public class InventoryBatch
    {
        public int BatchId { get; set; }
        public int ProductId { get; set; }
        public string BatchCode { get; set; } = null!;
        public DateOnly ManufactureDate { get; set; }
        public DateOnly ExpiryDate { get; set; }
        public int Quantity { get; set; }
        public decimal CostPrice { get; set; }
        public DateTime? CreatedAt { get; set; }

        public Product Product { get; set; } = null!;
    }
}
