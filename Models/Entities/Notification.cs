using System;

namespace OSBIS.Models.Entities
{
    /// <summary>
    /// Thông báo trong app (in-app notification bell trên navbar).
    /// Types: OrderPlaced, OrderConfirmed, OrderShipped, OrderDelivered, VoucherAvailable, System.
    /// </summary>
    public class Notification
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }

        /// <summary>Loại notification dùng để icon + routing</summary>
        public string Type { get; set; } = null!;

        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;

        /// <summary>URL khi click vào notification (optional)</summary>
        public string? LinkUrl { get; set; }

        public bool? IsRead { get; set; }
        public DateTime? CreatedAt { get; set; }

        public User User { get; set; } = null!;
    }
}
