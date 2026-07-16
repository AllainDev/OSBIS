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
        public async Task GenerateExpiringBatchNotificationsAsync(int? userId = null)
        {
            var expiringBatches = await _uow.InventoryBatches.GetExpiringSoonAsync(30);
            if (expiringBatches.Count == 0) return;

            List<int> targetUsers;
            if (userId.HasValue)
            {
                targetUsers = new List<int> { userId.Value };
            }
            else
            {
                var staffUsers = await _uow.Users.GetUsersByRoleAsync("Staff");
                var adminUsers = await _uow.Users.GetUsersByRoleAsync("Admin");
                targetUsers = staffUsers.Concat(adminUsers).Select(u => u.UserId).Distinct().ToList();
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            foreach (var uid in targetUsers)
            {
                // Retrieve recent notifications for this user to avoid duplicates
                var latestNotifs = await _uow.Notifications.GetLatestByUserAsync(uid, 100);

                foreach (var batch in expiringBatches)
                {
                    var isExpired = batch.ExpiryDate < today;
                    var type = $"ExpiringBatch_{batch.BatchId}";
                    var typeExpired = $"ExpiredBatch_{batch.BatchId}";

                    // Nếu đã thông báo expired rồi thì thôi
                    if (latestNotifs.Any(n => n.Type == typeExpired)) continue;

                    // Nếu chưa hết hạn, và đã thông báo expiring rồi thì thôi
                    if (!isExpired && latestNotifs.Any(n => n.Type == type)) continue;

                    // Đảm bảo không spam quá nhiều
                    if (isExpired)
                    {
                        var productName = batch.Product?.ProductName ?? "Sản phẩm";
                        await CreateAsync(
                            uid,
                            typeExpired,
                            $"Lô {batch.BatchCode} ĐÃ HẾT HẠN",
                            $"Lô hàng {batch.BatchCode} của {productName} đã hết hạn vào {batch.ExpiryDate:dd/MM/yyyy}. Yêu cầu tiêu hủy!",
                            $"/Staff/Product/Batches/{batch.ProductId}"
                        );
                    }
                    else
                    {
                        var productName = batch.Product?.ProductName ?? "Sản phẩm";
                        await CreateAsync(
                            uid,
                            type,
                            $"Cảnh báo lô {batch.BatchCode} sắp hỏng",
                            $"Lô hàng {batch.BatchCode} của {productName} sẽ hết hạn vào {batch.ExpiryDate:dd/MM/yyyy}. Vui lòng kiểm tra kho!",
                            $"/Staff/Product/Batches/{batch.ProductId}"
                        );
                    }
                }
            }
        }
    }
}
