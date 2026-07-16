using OSBIS.Common;
using OSBIS.Models.Entities;
using OSBIS.Models.Enums;
using OSBIS.Repositories.Interfaces;
using OSBIS.Services.Interfaces;
using Serilog;

namespace OSBIS.Services.Implementations
{
    /// <summary>
    /// Service quản lý Shipment — Phase 4.
    /// Đã được tích hợp Phase 5: gọi INotificationService + IEmailService.
    /// Đã fix bug: tính TotalWeight từ product.Weight thực tế (không hard-code 0.5m).
    /// Đã fix bug: FailedDelivery KHÔNG cancel order mà giữ ở trạng thái Shipped.
    /// </summary>
    public class ShipmentService : IShipmentService
    {
        private readonly IUnitOfWork _uow;
        private readonly IPaymentService _paymentService;
        private readonly IAuditLogService _auditLogService;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;

        public ShipmentService(
            IUnitOfWork uow,
            IPaymentService paymentService,
            IAuditLogService auditLogService,
            INotificationService notificationService,
            IEmailService emailService)
        {
            _uow = uow;
            _paymentService = paymentService;
            _auditLogService = auditLogService;
            _notificationService = notificationService;
            _emailService = emailService;
        }

        public async Task<Shipment?> GetByIdAsync(int shipmentId)
        {
            return await _uow.Shipments.GetByIdAsync(shipmentId);
        }

        public async Task<Shipment?> GetWithTrackingsAsync(int shipmentId)
        {
            return await _uow.Shipments.GetWithTrackingsAsync(shipmentId);
        }

        public async Task<Shipment?> GetByOrderIdAsync(int orderId)
        {
            return await _uow.Shipments.GetByOrderIdAsync(orderId);
        }

        public async Task<IReadOnlyList<ShipmentTracking>> GetTrackingHistoryAsync(int shipmentId)
        {
            return await _uow.ShipmentTrackings.GetByShipmentIdAsync(shipmentId);
        }

        public async Task<PagedResult<Shipment>> GetPagedAsync(int pageNumber, int pageSize, ShipmentStatus? status = null)
        {
            return await _uow.Shipments.GetPagedAsync(pageNumber, pageSize, status);
        }

        public async Task<IReadOnlyList<Shipment>> GetByShipperAsync(int shipperId, ShipmentStatus? status = null)
        {
            return await _uow.Shipments.GetByShipperAsync(shipperId, status);
        }

        public async Task<ShipmentResult> CreateShipmentAsync(int orderId, int? shipperId, int staffId)
        {
            try
            {
                await _uow.BeginTransactionAsync();

                var order = await _uow.Orders.GetWithDetailsAsync(orderId);
                if (order == null)
                {
                    await _uow.RollbackTransactionAsync();
                    return new ShipmentResult { Success = false, Message = "Không tìm thấy đơn hàng." };
                }

                if (order.OrderStatus != OrderStatus.Confirmed && order.OrderStatus != OrderStatus.Processing)
                {
                    await _uow.RollbackTransactionAsync();
                    return new ShipmentResult
                    {
                        Success = false,
                        Message = $"Không thể tạo shipment cho đơn ở trạng thái '{order.OrderStatus}'. Cần Confirmed/Processing."
                    };
                }

                // Check shipment đã tồn tại chưa
                var existing = await _uow.Shipments.GetByOrderIdAsync(orderId);
                if (existing != null)
                {
                    await _uow.RollbackTransactionAsync();
                    return new ShipmentResult { Success = false, Message = "Đơn hàng này đã có shipment." };
                }

                // ✅ FIX: Tính TotalWeight từ product.Weight thực tế (join qua OrderDetail.ProductId)
                decimal totalWeight = 0m;
                foreach (var detail in order.OrderDetails)
                {
                    var product = await _uow.Products.GetByIdAsync(detail.ProductId);
                    if (product != null)
                    {
                        totalWeight += product.Weight * detail.Quantity;
                    }
                    else
                    {
                        // Fallback nếu product bị xóa: dùng 0.5kg/SP (an toàn)
                        totalWeight += 0.5m * detail.Quantity;
                    }
                }

                var trackingNumber = $"SHIP-{DateTime.UtcNow:yyyyMMddHHmmss}";

                var shipment = new Shipment
                {
                    OrderId = orderId,
                    LogisticsProvider = "Shipper nội bộ",
                    TrackingNumber = trackingNumber,
                    TotalWeight = totalWeight,
                    EstimatedDeliveryDate = DateTime.UtcNow.AddDays(2),
                    ShipmentStatus = ShipmentStatus.Pending,
                    AssignedShipperId = shipperId,
                    UpdatedAt = DateTime.UtcNow
                };

                await _uow.Shipments.AddAsync(shipment);
                await _uow.SaveChangesAsync(); // để có ShipmentId

                // Tạo tracking đầu tiên
                var firstTracking = new ShipmentTracking
                {
                    ShipmentId = shipment.ShipmentId,
                    Status = ShipmentStatus.Pending,
                    Location = "Kho OSBIS",
                    Note = "Đã tạo vận đơn",
                    UpdatedBy = staffId,
                    UpdatedAt = DateTime.UtcNow
                };
                await _uow.ShipmentTrackings.AddAsync(firstTracking);

                // Order: Confirmed/Processing → Shipped
                order.OrderStatus = OrderStatus.Shipped;
                order.UpdatedAt = DateTime.UtcNow;
                _uow.Orders.Update(order);

                await _uow.CommitTransactionAsync();

                await _auditLogService.LogAsync(
                    staffId, null,
                    AuditAction.OrderConfirmed,
                    description: $"Shipment created for order {order.OrderCode}",
                    isSuccess: true,
                    controller: "Staff/Order",
                    actionName: "CreateShipment"
                );

                // ✅ FIX: Gọi NotificationService + EmailService (Phase 5 tích hợp)
                _ = SafeNotifyAsync(() => _notificationService.NotifyOrderShippedAsync(order),
                    $"Notify shipment for order {order.OrderCode}");
                _ = SafeEmailAsync(() => _emailService.SendShippingUpdateAsync(order, "Đơn hàng đã được tạo vận đơn và chuẩn bị giao."),
                    $"Email shipment for order {order.OrderCode}");

                return new ShipmentResult
                {
                    Success = true,
                    Message = "Tạo vận đơn thành công.",
                    Shipment = shipment
                };
            }
            catch (Exception ex)
            {
                await _uow.RollbackTransactionAsync();
                Log.Error(ex, "CreateShipment failed: orderId={OrderId}", orderId);
                return new ShipmentResult { Success = false, Message = "Có lỗi xảy ra." };
            }
        }

        public async Task<ShipmentResult> UpdateStatusAsync(int shipmentId, ShipmentStatus newStatus, string? location, string? note, int updatedBy)
        {
            Shipment? shipment = null;
            Order? order = null;

            try
            {
                await _uow.BeginTransactionAsync();

                shipment = await _uow.Shipments.GetWithTrackingsAsync(shipmentId);
                if (shipment == null || shipment.Order == null)
                {
                    await _uow.RollbackTransactionAsync();
                    return new ShipmentResult { Success = false, Message = "Không tìm thấy shipment." };
                }

                shipment.ShipmentStatus = newStatus;
                shipment.UpdatedAt = DateTime.UtcNow;
                _uow.Shipments.Update(shipment);

                // Tracking mới
                var tracking = new ShipmentTracking
                {
                    ShipmentId = shipmentId,
                    Status = newStatus,
                    Location = location ?? shipment.Order.ShippingAddress,
                    Note = note ?? StatusToNote(newStatus),
                    UpdatedBy = updatedBy,
                    UpdatedAt = DateTime.UtcNow
                };
                await _uow.ShipmentTrackings.AddAsync(tracking);

                // ✅ FIX: Mapping sang Order status chính xác hơn
                // - Pending/PickedUp/InTransit/OutForDelivery → Order vẫn Shipped
                // - Delivered → Order.OrderStatus = Delivered
                // - FailedDelivery/Returning → KHÔNG cancel đơn (giữ Shipped + xử lý refund riêng ở PaymentService)
                // - Returned → Order.OrderStatus = Returned
                order = shipment.Order;
                order.OrderStatus = newStatus switch
                {
                    ShipmentStatus.Pending => OrderStatus.Shipped,
                    ShipmentStatus.PickedUp => OrderStatus.Shipped,
                    ShipmentStatus.InTransit => OrderStatus.Shipped,
                    ShipmentStatus.OutForDelivery => OrderStatus.Shipped,
                    ShipmentStatus.Delivered => OrderStatus.Delivered,
                    ShipmentStatus.FailedDelivery => OrderStatus.Shipped, // ✅ FIX: Không cancel, giữ Shipped
                    ShipmentStatus.Returning => OrderStatus.Shipped,
                    ShipmentStatus.Returned => OrderStatus.Returned,
                    _ => order.OrderStatus
                };
                order.UpdatedAt = DateTime.UtcNow;
                _uow.Orders.Update(order);

                await _uow.CommitTransactionAsync();

                // ✅ FIX: Gọi Notification + Email sau khi commit thành công
                if (newStatus == ShipmentStatus.Delivered)
                {
                    _ = SafeNotifyAsync(() => _notificationService.NotifyOrderDeliveredAsync(order!),
                        $"Notify delivered for order {order!.OrderCode}");
                }
                else
                {
                    _ = SafeNotifyAsync(() => _notificationService.NotifyOrderShippedAsync(order!),
                        $"Notify shipment update for order {order!.OrderCode}");
                }

                _ = SafeEmailAsync(() => _emailService.SendShippingUpdateAsync(order!, StatusToNote(newStatus)),
                    $"Email shipment update for order {order!.OrderCode}");

                return new ShipmentResult
                {
                    Success = true,
                    Message = $"Cập nhật trạng thái: {newStatus}.",
                    Shipment = shipment
                };
            }
            catch (Exception ex)
            {
                await _uow.RollbackTransactionAsync();
                Log.Error(ex, "UpdateStatus failed: shipmentId={ShipmentId}", shipmentId);
                return new ShipmentResult { Success = false, Message = "Có lỗi xảy ra." };
            }
        }

        public async Task<ShipmentResult> ConfirmCODReceivedAsync(int shipmentId, int shipperId)
        {
            try
            {
                await _uow.BeginTransactionAsync();

                var shipment = await _uow.Shipments.GetByIdAsync(shipmentId);
                if (shipment == null)
                {
                    await _uow.RollbackTransactionAsync();
                    return new ShipmentResult { Success = false, Message = "Không tìm thấy shipment." };
                }

                // Lấy payment
                var payment = await _uow.Payments.GetByOrderIdAsync(shipment.OrderId);
                if (payment == null)
                {
                    await _uow.RollbackTransactionAsync();
                    return new ShipmentResult { Success = false, Message = "Không tìm thấy payment." };
                }

                if (payment.PaymentMethod != PaymentMethod.COD)
                {
                    await _uow.RollbackTransactionAsync();
                    return new ShipmentResult { Success = false, Message = "Đơn này không phải COD." };
                }

                // Xác nhận COD thu tiền thành công
                await _uow.CommitTransactionAsync(); // commit trước khi gọi PaymentService

                var result = await _paymentService.ConfirmCODReceivedAsync(payment.PaymentId, shipperId);
                return new ShipmentResult
                {
                    Success = result.Success,
                    Message = result.Message,
                    Shipment = shipment
                };
            }
            catch (Exception ex)
            {
                await _uow.RollbackTransactionAsync();
                Log.Error(ex, "ConfirmCODReceived failed: shipmentId={ShipmentId}", shipmentId);
                return new ShipmentResult { Success = false, Message = "Có lỗi xảy ra." };
            }
        }

        public async Task<ShipmentResult> AssignShipperAsync(int shipmentId, int shipperId, int staffId)
        {
            try
            {
                await _uow.BeginTransactionAsync();

                var shipment = await _uow.Shipments.GetByIdAsync(shipmentId);
                if (shipment == null)
                {
                    await _uow.RollbackTransactionAsync();
                    return new ShipmentResult { Success = false, Message = "Không tìm thấy shipment." };
                }

                shipment.AssignedShipperId = shipperId;
                shipment.UpdatedAt = DateTime.UtcNow;
                _uow.Shipments.Update(shipment);

                await _uow.CommitTransactionAsync();

                return new ShipmentResult
                {
                    Success = true,
                    Message = "Đã gán shipper.",
                    Shipment = shipment
                };
            }
            catch (Exception ex)
            {
                await _uow.RollbackTransactionAsync();
                Log.Error(ex, "AssignShipper failed");
                return new ShipmentResult { Success = false, Message = "Có lỗi xảy ra." };
            }
        }

        private static string StatusToNote(ShipmentStatus s) => s switch
        {
            ShipmentStatus.Pending => "Đang chờ lấy hàng",
            ShipmentStatus.PickedUp => "Shipper đã lấy hàng",
            ShipmentStatus.InTransit => "Đang vận chuyển",
            ShipmentStatus.OutForDelivery => "Đang giao hàng",
            ShipmentStatus.Delivered => "Đã giao thành công",
            ShipmentStatus.FailedDelivery => "Giao thất bại - chờ xử lý",
            ShipmentStatus.Returning => "Đang trả về kho",
            ShipmentStatus.Returned => "Đã trả về kho",
            _ => ""
        };

        // ✅ Helper: gọi notification mà KHÔNG làm fail cả flow chính
        private static async Task SafeNotifyAsync(Func<Task> action, string context)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Notification fire-and-forget] {Context}", context);
            }
        }

        private static async Task SafeEmailAsync(Func<Task> action, string context)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Email fire-and-forget] {Context}", context);
            }
        }
    }
}