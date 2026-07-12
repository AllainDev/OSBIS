namespace ORBIS.Models.Entities
{
    public class OrderDetail
    {
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string ProductNameSnapshot { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        // LineTotal is a computed column, we can ignore it for inserts or let EF handle it as computed

        public Order Order { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
