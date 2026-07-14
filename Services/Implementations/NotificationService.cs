using OSBIS.Common;
using OSBIS.Models.Entities;
using OSBIS.Models.Enums;
using OSBIS.Repositories.Interfaces;
using OSBIS.Services.Interfaces;
using Serilog;

namespace OSBIS.Services.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _uow;
        private readonly IAuditLogService _auditLogService;

        public NotificationService(IUnitOfWork uow, IAuditLogService auditLogService)
        {
            _uow = uow;
            _auditLogService = auditLogService;
        }

        public async Task<PagedResult<Notification>> GetByUserAsync(int userId, int pageNumber, int pageSize)
            => await _uow.Notifications.GetByUserAsync(userId, pageNumber, pageSize);

        public async Task<IReadOnlyList<Notification>> GetLatestAsync(int userId, int count = 5)
            => await _uow.Notifications.GetLatestByUserAsync(userId, count);

        public async Task<int> GetUnreadCountAsync(int userId)
            => await _uow.Notifications.GetUnreadCountAsync(userId);

        public async Task MarkAsReadAsync(int notificationId, int userId)
        {
            var notif = await _uow.Notifications.GetByIdAsync(notificationId);
            if (notif == null || notif.UserId != userId) return;

            notif.IsRead = true;
            _uow.Notifications.Update(notif);
            await _uow.SaveChangesAsync();
        }

        public async Task CreateAsync(int userId, string type, string title, string message, string? linkUrl = null)
        {
            var notif = new Notification
            {
                UserId = userId,
                Type = type,
                Title = title,
                Message = message,
                LinkUrl = linkUrl,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            await _uow.Notifications.AddAsync(notif);
            await _uow.SaveChangesAsync();
        }

        public async Task NotifyOrderPlacedAsync(Order order)
        {
            await CreateAsync(
                order.UserId,
                "OrderPlaced",
                "Đặt hàng thành công",
                $"Đơn hàng {order.OrderCode} trị giá {order.TotalAmount:N0}đ đã được tạo.",
                $"/Customer/Order/Detail/{order.OrderId}");
        }

        public async Task NotifyOrderShippedAsync(Order order)
        {
            await CreateAsync(
                order.UserId,
                "OrderShipped",
                "Đơn hàng đang được giao",
                $"Đơn hàng {order.OrderCode} đang trên đường giao đến bạn.",
                $"/Customer/Order/Tracking/{order.OrderId}");
        }

        public async Task NotifyOrderDeliveredAsync(Order order)
        {
            await CreateAsync(
                order.UserId,
                "OrderDelivered",
                "Đơn hàng đã giao thành công",
                $"Đơn hàng {order.OrderCode} đã được giao. Cảm ơn bạn đã mua hàng!",
                $"/Customer/Order/Detail/{order.OrderId}");
        }

        public async Task NotifyVoucherAvailableAsync(int userId, Voucher voucher)
        {
            await CreateAsync(
                userId,
                "VoucherAvailable",
                "Bạn có voucher mới!",
                $"Voucher {voucher.VoucherCode} - {voucher.DiscountValue}% giảm giá.",
                "/");
        }
    }
}
