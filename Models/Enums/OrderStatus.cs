namespace OSBIS.Models.Enums
{
    /// <summary>
    /// Trạng thái đơn hàng (BR - Business Rules)
    /// </summary>
    public enum OrderStatus : byte
    {
        Pending = 0,        // Chờ xác nhận
        Confirmed = 1,      // Đã xác nhận (payment success)
        Processing = 2,     // Đang xử lý (đóng gói)
        Shipped = 3,        // Đã giao cho vận chuyển
        Delivered = 4,      // Đã giao thành công
        Completed = 5,      // Hoàn tất (customer nhận & thanh toán xong)
        Cancelled = 6,      // Đã hủy
        Returned = 7,       // Trả hàng
        Refunded = 8        // Đã hoàn tiền
    }
}
