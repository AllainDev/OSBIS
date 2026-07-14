using OSBIS.Common;
using OSBIS.Models.Entities;
using OSBIS.Models.Enums;
using OSBIS.Models.ViewModels.Checkout;

namespace OSBIS.Services.Interfaces
{
    /// <summary>
    /// Service quản lý Order:
    /// - PlaceOrderAsync: tạo Order + OrderDetail + Payment từ giỏ hàng
    /// - CancelOrderAsync: hủy order (chỉ khi Pending/Confirmed/Processing)
    /// - GetOrders/GetOrder cho Customer xem
    /// - Phase 4: thêm Staff operations
    /// </summary>
    public interface IOrderService
    {
        Task<PlaceOrderResult> PlaceOrderAsync(int userId, CheckoutViewModel dto);
        Task<CancelOrderResult> CancelOrderAsync(int orderId, int userId);

        Task<PagedResult<Order>> GetUserOrdersAsync(int userId, int pageNumber, int pageSize);
        Task<Order?> GetOrderDetailAsync(int orderId);
        Task<Order?> GetOrderByCodeAsync(string code);

        // ====== Phase 4: Staff operations ======
        Task<PagedResult<Order>> GetPagedAsync(int pageNumber, int pageSize, OrderStatus? status = null, string? search = null);
        Task<Order?> GetForStaffAsync(int orderId);
        Task<OrderActionResult> ConfirmOrderAsync(int orderId, int staffId);
    }

    public class PlaceOrderResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Order? Order { get; set; }
    }

    public class CancelOrderResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class OrderActionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Order? Order { get; set; }
    }
}
