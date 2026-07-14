using System.Collections.Generic;
using OSBIS.Common;

namespace OSBIS.Models.ViewModels.Order
{
    /// <summary>VM cho trang danh sách đơn hàng của customer.</summary>
    public class OrderListViewModel
    {
        public PagedResult<OSBIS.Models.Entities.Order> Orders { get; set; } = new();
        public string? StatusFilter { get; set; }
    }
}
