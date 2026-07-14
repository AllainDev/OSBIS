using OSBIS.Models.Entities;

namespace OSBIS.Repositories.Interfaces
{
    /// <summary>
    /// Unit of Work Pattern.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        // Phase 1
        IUserRepository Users { get; }
        IRoleRepository Roles { get; }
        IUserAddressRepository UserAddresses { get; }
        IAuditLogRepository AuditLogs { get; }

        // Phase 2
        IProductRepository Products { get; }
        ICategoryRepository Categories { get; }
        IInventoryBatchRepository InventoryBatches { get; }
        IProductImageRepository ProductImages { get; }

        // Phase 3
        ICartRepository Carts { get; }
        IOrderRepository Orders { get; }
        IOrderDetailRepository OrderDetails { get; }
        IPaymentRepository Payments { get; }
        IVoucherRepository Vouchers { get; }
        IVoucherUsageRepository VoucherUsages { get; }
        ISystemConfigRepository SystemConfigs { get; }

        // Phase 4
        IShipmentRepository Shipments { get; }
        IShipmentTrackingRepository ShipmentTrackings { get; }
        IReviewRepository Reviews { get; }

        // Phase 5
        INotificationRepository Notifications { get; }

        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
