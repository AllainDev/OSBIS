using System.Collections.Generic;

namespace OSBIS.Models.ViewModels.Order
{
    /// <summary>VM cho trang chi tiết đơn hàng.</summary>
    public class OrderDetailViewModel
    {
        public OSBIS.Models.Entities.Order Order { get; set; } = null!;
        public List<OrderDetailItemViewModel> Items { get; set; } = new();
        public bool CanCancel { get; set; }
    }

    public class OrderDetailItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal => UnitPrice * Quantity;
        public bool CanReview { get; set; }
    }
}
