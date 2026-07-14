namespace OSBIS.Models.Enums
{
    /// <summary>
    /// Trạng thái giao dịch thanh toán
    /// </summary>
    public enum PaymentStatus : byte
    {
        Pending = 0,        // Chờ thanh toán
        Processing = 1,     // Đang xử lý
        Completed = 2,      // Thành công
        Failed = 3,         // Thất bại
        Refunded = 4,       // Đã hoàn tiền
        Cancelled = 5       // Đã hủy
    }
}