using Microsoft.EntityFrameworkCore.Storage;
using OSBIS.Data;
using OSBIS.Repositories.Interfaces;

namespace OSBIS.Repositories.Implementations
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IDbContextTransaction? _transaction;

        // Phase 1
        private IUserRepository? _userRepository;
        private IRoleRepository? _roleRepository;
        private IUserAddressRepository? _userAddressRepository;
        private IAuditLogRepository? _auditLogRepository;

        // Phase 2
        private IProductRepository? _productRepository;
        private ICategoryRepository? _categoryRepository;
        private IInventoryBatchRepository? _inventoryBatchRepository;
        private IProductImageRepository? _productImageRepository;

        // Phase 3
        private ICartRepository? _cartRepository;
        private IOrderRepository? _orderRepository;
        private IOrderDetailRepository? _orderDetailRepository;
        private IPaymentRepository? _paymentRepository;
        private IVoucherRepository? _voucherRepository;
        private IVoucherUsageRepository? _voucherUsageRepository;
        private ISystemConfigRepository? _systemConfigRepository;

        // Phase 4
        private IShipmentRepository? _shipmentRepository;
        private IShipmentTrackingRepository? _shipmentTrackingRepository;
        private IReviewRepository? _reviewRepository;

        // Phase 5
        private INotificationRepository? _notificationRepository;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public IUserRepository Users => _userRepository ??= new UserRepository(_context);
        public IRoleRepository Roles => _roleRepository ??= new RoleRepository(_context);
        public IUserAddressRepository UserAddresses => _userAddressRepository ??= new UserAddressRepository(_context);
        public IAuditLogRepository AuditLogs => _auditLogRepository ??= new AuditLogRepository(_context);

        public IProductRepository Products => _productRepository ??= new ProductRepository(_context);
        public ICategoryRepository Categories => _categoryRepository ??= new CategoryRepository(_context);
        public IInventoryBatchRepository InventoryBatches => _inventoryBatchRepository ??= new InventoryBatchRepository(_context);
        public IProductImageRepository ProductImages => _productImageRepository ??= new ProductImageRepository(_context);

        public ICartRepository Carts => _cartRepository ??= new CartRepository(_context);
        public IOrderRepository Orders => _orderRepository ??= new OrderRepository(_context);
        public IOrderDetailRepository OrderDetails => _orderDetailRepository ??= new OrderDetailRepository(_context);
        public IPaymentRepository Payments => _paymentRepository ??= new PaymentRepository(_context);
        public IVoucherRepository Vouchers => _voucherRepository ??= new VoucherRepository(_context);
        public IVoucherUsageRepository VoucherUsages => _voucherUsageRepository ??= new VoucherUsageRepository(_context);
        public ISystemConfigRepository SystemConfigs => _systemConfigRepository ??= new SystemConfigRepository(_context);

        public IShipmentRepository Shipments => _shipmentRepository ??= new ShipmentRepository(_context);
        public IShipmentTrackingRepository ShipmentTrackings => _shipmentTrackingRepository ??= new ShipmentTrackingRepository(_context);
        public IReviewRepository Reviews => _reviewRepository ??= new ReviewRepository(_context);

        public INotificationRepository Notifications => _notificationRepository ??= new NotificationRepository(_context);

        public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
                if (_transaction != null) await _transaction.CommitAsync();
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
