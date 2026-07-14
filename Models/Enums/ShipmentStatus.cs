namespace OSBIS.Models.Enums
{
    /// <summary>
    /// Trạng thái vận chuyển
    /// </summary>
    public enum ShipmentStatus : byte
    {
        Pending = 0,        // Chờ lấy hàng
        PickedUp = 1,       // Đã lấy hàng
        InTransit = 2,      // Đang vận chuyển
        OutForDelivery = 3, // Đang giao hàng
        Delivered = 4,      // Đã giao thành công
        FailedDelivery = 5, // Giao thất bại
        Returning = 6,      // Đang trả về
        Returned = 7        // Đã trả về kho
    }
}