namespace OSBIS.Models.ViewModels.Cart
{
    /// <summary>Một dòng trong giỏ hàng (dùng để hiển thị).</summary>
    public class CartItemViewModel
    {
        public int CartItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public int AvailableStock { get; set; }
        public decimal Weight { get; set; } // (kg) dùng để tính phí ship
        public decimal LineTotal => UnitPrice * Quantity;
        public decimal LineWeight => Weight * Quantity;
    }
}
