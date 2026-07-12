using Microsoft.EntityFrameworkCore;
using ORBIS.Models.Entities;

namespace ORBIS.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<UserAddress> UserAddresses { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<ProductImage> ProductImages { get; set; } = null!;
        public DbSet<InventoryBatch> InventoryBatches { get; set; } = null!;
        public DbSet<Voucher> Vouchers { get; set; } = null!;
        public DbSet<Cart> Carts { get; set; } = null!;
        public DbSet<CartItem> CartItems { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderDetail> OrderDetails { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<Shipment> Shipments { get; set; } = null!;
        public DbSet<Review> Reviews { get; set; } = null!;
        public DbSet<PendingRegistration> PendingRegistrations
    => Set<PendingRegistration>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Table mappings
            modelBuilder.Entity<Role>().ToTable("Role");
            modelBuilder.Entity<User>().ToTable("User");
            modelBuilder.Entity<UserAddress>().ToTable("UserAddress");
            modelBuilder.Entity<Category>().ToTable("Category");
            modelBuilder.Entity<Product>().ToTable("Product");
            modelBuilder.Entity<ProductImage>().ToTable("ProductImage");
            modelBuilder.Entity<InventoryBatch>().ToTable("InventoryBatch");
            modelBuilder.Entity<Voucher>().ToTable("Voucher");
            modelBuilder.Entity<Cart>().ToTable("Cart");
            modelBuilder.Entity<CartItem>().ToTable("CartItem");
            modelBuilder.Entity<Order>().ToTable("Order");
            modelBuilder.Entity<OrderDetail>().ToTable("OrderDetail");
            modelBuilder.Entity<Payment>().ToTable("Payment");
            modelBuilder.Entity<Shipment>().ToTable("Shipment");
            modelBuilder.Entity<Review>().ToTable("Review");
            modelBuilder.Entity<PendingRegistration>().ToTable("PendingRegistration");

            // Roles
            modelBuilder.Entity<Role>()
                .HasIndex(r => r.RoleName)
                .IsUnique();

            // Users
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
            modelBuilder.Entity<User>()
                .Property(u => u.IsActive)
                .HasDefaultValue(true);
            modelBuilder.Entity<User>()
                .Property(u => u.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
            modelBuilder.Entity<User>()
                .Property(u => u.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // PendingRegistration
            modelBuilder.Entity<PendingRegistration>()
                .HasKey(pr => pr.PendingRegistrationId);

            modelBuilder.Entity<PendingRegistration>()
                .HasIndex(pr => pr.Email)
                .IsUnique();

            modelBuilder.Entity<PendingRegistration>()
                .Property(pr => pr.FullName)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<PendingRegistration>()
                .Property(pr => pr.Email)
                .IsRequired()
                .HasMaxLength(256);

            modelBuilder.Entity<PendingRegistration>()
                .Property(pr => pr.Username)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<PendingRegistration>()
                .Property(pr => pr.PasswordHash)
                .IsRequired()
                .HasMaxLength(500);

            modelBuilder.Entity<PendingRegistration>()
                .Property(pr => pr.OtpHash)
                .IsRequired()
                .HasMaxLength(128);

            modelBuilder.Entity<PendingRegistration>()
                .Property(pr => pr.FailedAttempts)
                .HasDefaultValue(0);

            modelBuilder.Entity<PendingRegistration>()
                .Property(pr => pr.CreatedAtUtc)
                .HasDefaultValueSql("GETUTCDATE()");

            // UserAddress
            modelBuilder.Entity<UserAddress>()
                .HasKey(ua => ua.AddressId);
            modelBuilder.Entity<UserAddress>()
                .Property(ua => ua.IsDefault)
                .HasDefaultValue(false);
            modelBuilder.Entity<UserAddress>()
                .HasOne(ua => ua.User)
                .WithMany(u => u.UserAddresses)
                .HasForeignKey(ua => ua.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Category
            modelBuilder.Entity<Category>()
                .Property(c => c.IsDeleted)
                .HasDefaultValue(false);
            modelBuilder.Entity<Category>()
                .Property(c => c.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
            modelBuilder.Entity<Category>()
                .Property(c => c.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
            modelBuilder.Entity<Category>()
                .HasOne(c => c.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Product
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.SKU)
                .IsUnique();
            modelBuilder.Entity<Product>()
                .Property(p => p.Weight)
                .HasColumnType("decimal(10,2)");
            modelBuilder.Entity<Product>()
                .Property(p => p.UnitPrice)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Product>()
                .Property(p => p.TotalStock)
                .HasDefaultValue(0);
            modelBuilder.Entity<Product>()
                .Property(p => p.ReservedQuantity)
                .HasDefaultValue(0);
            modelBuilder.Entity<Product>()
                .Property(p => p.IsDeleted)
                .HasDefaultValue(false);
            modelBuilder.Entity<Product>()
                .Property(p => p.RowVersion)
                .IsRowVersion();
            modelBuilder.Entity<Product>()
                .Property(p => p.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
            modelBuilder.Entity<Product>()
                .Property(p => p.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // ProductImage
            modelBuilder.Entity<ProductImage>()
                .HasKey(pi => pi.ImageId);
            modelBuilder.Entity<ProductImage>()
                .Property(pi => pi.IsPrimary)
                .HasDefaultValue(false);
            modelBuilder.Entity<ProductImage>()
                .Property(pi => pi.SortOrder)
                .HasDefaultValue((byte)0);
            modelBuilder.Entity<ProductImage>()
                .HasOne(pi => pi.Product)
                .WithMany(p => p.ProductImages)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // InventoryBatch
            modelBuilder.Entity<InventoryBatch>()
                .HasKey(ib => ib.BatchId);
            modelBuilder.Entity<InventoryBatch>()
                .Property(ib => ib.CostPrice)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<InventoryBatch>()
                .Property(ib => ib.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
            modelBuilder.Entity<InventoryBatch>()
                .HasOne(ib => ib.Product)
                .WithMany(p => p.InventoryBatches)
                .HasForeignKey(ib => ib.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Voucher
            modelBuilder.Entity<Voucher>()
                .HasIndex(v => v.VoucherCode)
                .IsUnique();
            modelBuilder.Entity<Voucher>()
                .Property(v => v.DiscountValue)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Voucher>()
                .Property(v => v.MinOrderValue)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0m);
            modelBuilder.Entity<Voucher>()
                .Property(v => v.MaxDiscountAmount)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Voucher>()
                .Property(v => v.UsedCount)
                .HasDefaultValue(0);
            modelBuilder.Entity<Voucher>()
                .Property(v => v.IsActive)
                .HasDefaultValue(true);

            // Cart
            modelBuilder.Entity<Cart>()
                .HasIndex(c => c.UserId)
                .IsUnique();
            modelBuilder.Entity<Cart>()
                .HasIndex(c => c.SessionId)
                .IsUnique();
            modelBuilder.Entity<Cart>()
                .Property(c => c.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
            modelBuilder.Entity<Cart>()
                .Property(c => c.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
            modelBuilder.Entity<Cart>()
                .HasOne(c => c.User)
                .WithOne(u => u.Cart)
                .HasForeignKey<Cart>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // CartItem
            modelBuilder.Entity<CartItem>()
                .HasIndex(ci => new { ci.CartId, ci.ProductId })
                .IsUnique();
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Cart)
                .WithMany(c => c.CartItems)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Product)
                .WithMany(p => p.CartItems)
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Order
            modelBuilder.Entity<Order>()
                .Property(o => o.OrderDate)
                .HasDefaultValueSql("GETUTCDATE()");
            modelBuilder.Entity<Order>()
                .Property(o => o.SubTotal)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Order>()
                .Property(o => o.ShippingFee)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Order>()
                .Property(o => o.DiscountAmount)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0m);
            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Order>()
                .Property(o => o.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Voucher)
                .WithMany(v => v.Orders)
                .HasForeignKey(o => o.VoucherId)
                .OnDelete(DeleteBehavior.SetNull);

            // OrderDetail
            modelBuilder.Entity<OrderDetail>()
                .HasKey(od => new { od.OrderId, od.ProductId });
            modelBuilder.Entity<OrderDetail>()
                .Property(od => od.UnitPrice)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Product)
                .WithMany(p => p.OrderDetails)
                .HasForeignKey(od => od.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Payment
            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.OrderId)
                .IsUnique();
            modelBuilder.Entity<Payment>()
                .Property(p => p.PaymentDate)
                .HasDefaultValueSql("GETUTCDATE()");
            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Payment>()
                .Property(p => p.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Order)
                .WithOne(o => o.Payment)
                .HasForeignKey<Payment>(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Shipment
            modelBuilder.Entity<Shipment>()
                .HasIndex(s => s.OrderId)
                .IsUnique();
            modelBuilder.Entity<Shipment>()
                .Property(s => s.TotalWeight)
                .HasColumnType("decimal(10,2)");
            modelBuilder.Entity<Shipment>()
                .Property(s => s.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
            modelBuilder.Entity<Shipment>()
                .HasOne(s => s.Order)
                .WithOne(o => o.Shipment)
                .HasForeignKey<Shipment>(s => s.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Review
            modelBuilder.Entity<Review>()
                .HasIndex(r => new { r.OrderId, r.ProductId })
                .IsUnique();
            modelBuilder.Entity<Review>()
                .Property(r => r.ReviewDate)
                .HasDefaultValueSql("GETUTCDATE()");
            modelBuilder.Entity<Review>()
                .Property(r => r.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Order)
                .WithMany(o => o.Reviews)
                .HasForeignKey(r => r.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

        }
    }
}