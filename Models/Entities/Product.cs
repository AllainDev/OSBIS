using System;
using System.Collections.Generic;

namespace ORBIS.Models.Entities
{
    public class Product
    {
        public int ProductId { get; set; }
        public int CategoryId { get; set; }
        public string SKU { get; set; } = null!;
        public string ProductName { get; set; } = null!;
        public string? Description { get; set; }
        public string UnitOfMeasure { get; set; } = null!;
        public decimal Weight { get; set; }
        public decimal UnitPrice { get; set; }
        public int TotalStock { get; set; }
        public int ReservedQuantity { get; set; }
        public bool? IsDeleted { get; set; }
        public byte[]? RowVersion { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public Category Category { get; set; } = null!;
        public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
        public ICollection<InventoryBatch> InventoryBatches { get; set; } = new List<InventoryBatch>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
