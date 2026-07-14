using System.Collections.Generic;

namespace OSBIS.Models.ViewModels.Cart
{
    /// <summary>ViewModel tổng hợp giỏ hàng: danh sách items + subtotal + quantity + total weight.</summary>
    public class CartSummaryViewModel
    {
        public int CartId { get; set; }
        public bool IsEmpty => Items == null || Items.Count == 0;
        public List<CartItemViewModel> Items { get; set; } = new();
        public int TotalQuantity => Items?.Sum(i => i.Quantity) ?? 0;
        public decimal SubTotal => Items?.Sum(i => i.LineTotal) ?? 0m;
        public decimal TotalWeight => Items?.Sum(i => i.LineWeight) ?? 0m;
    }
}
