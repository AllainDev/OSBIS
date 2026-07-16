using OSBIS.Common;
using OSBIS.Models.Entities;

namespace OSBIS.Services.Interfaces
{
    /// <summary>Service quản lý in-app notification (Phase 5).</summary>
    public interface INotificationService
    {
        Task<PagedResult<Notification>> GetByUserAsync(int userId, int pageNumber, int pageSize);
        Task<IReadOnlyList<Notification>> GetLatestAsync(int userId, int count = 5);
        Task<int> GetUnreadCountAsync(int userId);
        Task MarkAsReadAsync(int notificationId, int userId);

        Task CreateAsync(int userId, string type, string title, string message, string? linkUrl = null);
        Task NotifyOrderPlacedAsync(Order order);
        Task NotifyOrderShippedAsync(Order order);
        Task NotifyOrderDeliveredAsync(Order order);
        Task NotifyVoucherAvailableAsync(int userId, Voucher voucher);
        Task GenerateExpiringBatchNotificationsAsync(int? userId = null);
    }
}
