using System.ComponentModel.DataAnnotations.Schema;

namespace OSBIS.Models.Entities
{
    public class OrderDetail
    {
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string ProductNameSnapshot { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        /// <summary>Computed: Quantity * UnitPrice. Không lưu DB, tính runtime.</summary>
        [NotMapped]
        public decimal LineTotal => Quantity * UnitPrice;

        public Order Order { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
