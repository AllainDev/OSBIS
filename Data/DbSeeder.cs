using Microsoft.EntityFrameworkCore;
using OSBIS.Common;
using OSBIS.Models.Entities;
using OSBIS.Models.Enums;
using OSBIS.Services.Interfaces;

namespace OSBIS.Data
{
    /// <summary>
    /// Database Seeder — khởi tạo dữ liệu mặc định cho toàn bộ hệ thống.
    /// Phase 1: Roles + Admin/Customer.
    /// Phase 1.5: + Staff/Delivery/Sample Categories/Products/Voucher/SystemConfig.
    /// </summary>
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext context, IPasswordHasher passwordHasher)
        {
            await SeedRolesAsync(context);
            await SeedAdminUserAsync(context, passwordHasher);
            await SeedStaffUserAsync(context, passwordHasher);
            await SeedDeliveryUserAsync(context, passwordHasher);
            await SeedSampleCustomerAsync(context, passwordHasher);
            await SeedSystemConfigsAsync(context);
            await SeedCategoriesAsync(context);
            await SeedProductsAsync(context);
            await SeedSampleVoucherAsync(context);
        }

        // ============================================================
        // ROLES
        // ============================================================
        // RoleId column is tinyint (no IDENTITY) — cần tự gán hoặc đảm bảo chỉ insert 1 entity/lần.
        // Dùng ID tĩnh: Admin=1, Staff=2, Customer=3, Delivery=4.
        private const byte ROLE_ADMIN = 1;
        private const byte ROLE_STAFF = 2;
        private const byte ROLE_CUSTOMER = 3;
        private const byte ROLE_DELIVERY = 4;

        private static async Task SeedRolesAsync(AppDbContext context)
        {
            var rolesToSeed = new (byte Id, string Name)[]
            {
                (ROLE_ADMIN, AppConstants.Roles.Admin),
                (ROLE_STAFF, AppConstants.Roles.Staff),
                (ROLE_CUSTOMER, AppConstants.Roles.Customer),
                (ROLE_DELIVERY, AppConstants.Roles.Delivery)
            };

            var existing = await context.Roles.Select(r => r.RoleName).ToListAsync();
            var toAdd = rolesToSeed.Where(r => !existing.Contains(r.Name))
                                   .Select(r => new Role { RoleId = r.Id, RoleName = r.Name })
                                   .ToList();
            if (toAdd.Any())
            {
                context.Roles.AddRange(toAdd);
                await context.SaveChangesAsync();
            }
        }

        // ============================================================
        // USERS
        // ============================================================
        private static async Task SeedAdminUserAsync(AppDbContext context, IPasswordHasher passwordHasher)
        {
            const string adminUsername = "admin";
            if (await context.Users.AnyAsync(u => u.Username == adminUsername)) return;

            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == AppConstants.Roles.Admin);
            if (adminRole == null) return;

            context.Users.Add(new User
            {
                Username = adminUsername,
                PasswordHash = passwordHasher.HashPassword("Admin@123"),
                Email = "admin@osbis.com",
                FullName = "System Administrator",
                Phone = "0123456789",
                RoleId = adminRole.RoleId,
                IsActive = true,
                FailedLoginCount = 0
            });
            await context.SaveChangesAsync();
        }

        private static async Task SeedStaffUserAsync(AppDbContext context, IPasswordHasher passwordHasher)
        {
            const string username = "staff";
            if (await context.Users.AnyAsync(u => u.Username == username)) return;

            var role = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == AppConstants.Roles.Staff);
            if (role == null) return;

            context.Users.Add(new User
            {
                Username = username,
                PasswordHash = passwordHasher.HashPassword("Staff@123"),
                Email = "staff@osbis.com",
                FullName = "Nguyễn Văn Staff",
                Phone = "0912345678",
                RoleId = role.RoleId,
                IsActive = true,
                FailedLoginCount = 0
            });
            await context.SaveChangesAsync();
        }

        private static async Task SeedDeliveryUserAsync(AppDbContext context, IPasswordHasher passwordHasher)
        {
            const string username = "shipper1";
            if (await context.Users.AnyAsync(u => u.Username == username)) return;

            var role = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == AppConstants.Roles.Delivery);
            if (role == null) return;

            context.Users.Add(new User
            {
                Username = username,
                PasswordHash = passwordHasher.HashPassword("Shipper@123"),
                Email = "shipper1@osbis.com",
                FullName = "Trần Văn Shipper",
                Phone = "0933333333",
                RoleId = role.RoleId,
                IsActive = true,
                FailedLoginCount = 0
            });
            await context.SaveChangesAsync();
        }

        private static async Task SeedSampleCustomerAsync(AppDbContext context, IPasswordHasher passwordHasher)
        {
            const string username = "customer1";
            if (await context.Users.AnyAsync(u => u.Username == username)) return;

            var role = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == AppConstants.Roles.Customer);
            if (role == null) return;

            context.Users.Add(new User
            {
                Username = username,
                PasswordHash = passwordHasher.HashPassword("User@123"),
                Email = "customer1@example.com",
                FullName = "Khách hàng Demo",
                Phone = "0987654321",
                RoleId = role.RoleId,
                IsActive = true,
                FailedLoginCount = 0
            });
            await context.SaveChangesAsync();
        }

        // ============================================================
        // SYSTEM CONFIG (defaults cho shipping fee, sequence, expiry)
        // ============================================================
        private static async Task SeedSystemConfigsAsync(AppDbContext context)
        {
            var defaults = new Dictionary<string, (string Value, string Desc)>
            {
                ["ShippingFeePerKg"]   = ("25000", "Phí ship mỗi kg (VND)"),
                ["FreeShipThreshold"]  = ("500000", "Miễn ship nếu SubTotal >= threshold (VND)"),
                ["MinShippingFee"]     = ("30000", "Phí ship tối thiểu (VND)"),
                ["CartExpiryHours"]    = ("24", "Sau X giờ thì coi như cart abandoned"),
                ["OrderSequence"]      = ("0", "Sequence ID đơn hàng trong ngày (auto-reset)"),
                ["BankAccountName"]    = ("CONG TY TNHH GIA HOA PHAT", "Tên TK ngân hàng"),
                ["BankAccountNumber"]  = ("1234567890", "Số TK ngân hàng nhận CK"),
                ["BankName"]           = ("Vietcombank", "Tên ngân hàng")
            };

            foreach (var kv in defaults)
            {
                var exists = await context.SystemConfigs.AnyAsync(c => c.ConfigKey == kv.Key);
                if (!exists)
                {
                    context.SystemConfigs.Add(new SystemConfig
                    {
                        ConfigKey = kv.Key,
                        ConfigValue = kv.Value.Value,
                        Description = kv.Value.Desc
                    });
                }
            }
            await context.SaveChangesAsync();
        }

        // ============================================================
        // CATEGORIES (đa cấp: 4 cha + 6 con)
        // ============================================================
        private static async Task SeedCategoriesAsync(AppDbContext context)
        {
            if (await context.Categories.AnyAsync()) return;

            // Root categories
            var botBot = new Category { CategoryName = "Bột làm bánh", Description = "Các loại bột mì, bột năng, bột ngô..." };
            var nguyenLieu = new Category { CategoryName = "Nguyên liệu khác", Description = "Đường, sữa, bơ, trứng, men..." };
            var phamMau = new Category { CategoryName = "Phẩm màu & hương liệu", Description = "Màu thực phẩm, tinh chất vani..." };
            var dungCu = new Category { CategoryName = "Dụng cụ làm bánh", Description = "Khuôn, túi bắt kem, cây lăn..." };

            context.Categories.AddRange(botBot, nguyenLieu, phamMau, dungCu);
            await context.SaveChangesAsync();

            // Sub categories
            var subs = new[]
            {
                new Category { CategoryName = "Bột mì", Description = "Bột mì số 8, số 11...", ParentCategoryId = botBot.CategoryId },
                new Category { CategoryName = "Bột năng", Description = "Bột năng các loại", ParentCategoryId = botBot.CategoryId },
                new Category { CategoryName = "Đường & Sữa", Description = "Đường cát, đường phèn, sữa đặc, sữa tươi...", ParentCategoryId = nguyenLieu.CategoryId },
                new Category { CategoryName = "Bơ & Trứng", Description = "Bơ lạt, bơ unsalted, trứng gà...", ParentCategoryId = nguyenLieu.CategoryId },
                new Category { CategoryName = "Màu thực phẩm", Description = "Màu gel, màu dạng bột, màu dạng nước", ParentCategoryId = phamMau.CategoryId },
                new Category { CategoryName = "Khuôn bánh", Description = "Khuôn muffin, bánh kem, bánh nướng", ParentCategoryId = dungCu.CategoryId },
            };

            context.Categories.AddRange(subs);
            await context.SaveChangesAsync();
        }

        // ============================================================
        // SAMPLE PRODUCTS (15 items, rải đều 6 sub categories)
        // ============================================================
        private static async Task SeedProductsAsync(AppDbContext context)
        {
            if (await context.Products.AnyAsync()) return;

            var categories = await context.Categories.ToListAsync();
            Category? Find(string name) => categories.FirstOrDefault(c => c.CategoryName == name);

            var sampleProducts = new[]
            {
                new Product { SKU = "BTH-001", ProductName = "Bột mì số 8 (1kg)", Description = "Bột mì đa dụng số 8, thích hợp làm bánh mì, bánh ngọt", UnitOfMeasure = "gói", Weight = 1.0m, UnitPrice = 38000m, TotalStock = 100 },
                new Product { SKU = "BTH-002", ProductName = "Bột mì số 11 (1kg)", Description = "Bột mì số 11, bánh bông lan chuyên dụng", UnitOfMeasure = "gói", Weight = 1.0m, UnitPrice = 42000m, TotalStock = 80 },
                new Product { SKU = "BTH-003", ProductName = "Bột năng (500g)", Description = "Bột năng nguyên chất, làm bánh bột lọt, chè", UnitOfMeasure = "gói", Weight = 0.5m, UnitPrice = 28000m, TotalStock = 60 },
                new Product { SKU = "NL-001", ProductName = "Đường cát trắng (1kg)", Description = "Đường cát trắng tinh luyện", UnitOfMeasure = "gói", Weight = 1.0m, UnitPrice = 25000m, TotalStock = 200 },
                new Product { SKU = "NL-002", ProductName = "Sữa đặc Ông Thọ (380g)", Description = "Sữa đặc có đường, lon thiếc", UnitOfMeasure = "lon", Weight = 0.4m, UnitPrice = 32000m, TotalStock = 90 },
                new Product { SKU = "NL-003", ProductName = "Sữa tươi không đường (1L)", Description = "Sữa tươi Vinamilk 100% không đường", UnitOfMeasure = "hộp", Weight = 1.0m, UnitPrice = 35000m, TotalStock = 50 },
                new Product { SKU = "NL-004", ProductName = "Bơ lạt Anchor (250g)", Description = "Bơ lạt unsalted, thích hợp làm bánh", UnitOfMeasure = "gói", Weight = 0.25m, UnitPrice = 95000m, TotalStock = 40 },
                new Product { SKU = "NL-005", ProductName = "Trứng gà ta (hộp 10 quả)", Description = "Trứng gà ta, giàu dinh dưỡng", UnitOfMeasure = "hộp", Weight = 0.5m, UnitPrice = 45000m, TotalStock = 120 },
                new Product { SKU = "PM-001", ProductName = "Màu thực phẩm đỏ (10ml)", Description = "Màu gel đỏ, dùng trang trí bánh", UnitOfMeasure = "chai", Weight = 0.02m, UnitPrice = 22000m, TotalStock = 70 },
                new Product { SKU = "PM-002", ProductName = "Tinh chất vani (50ml)", Description = "Tinh chất vani nguyên chất", UnitOfMeasure = "chai", Weight = 0.05m, UnitPrice = 38000m, TotalStock = 55 },
                new Product { SKU = "PM-003", ProductName = "Bột ca cao nguyên chất (250g)", Description = "Bột cacao không đường, dùng làm bánh", UnitOfMeasure = "gói", Weight = 0.25m, UnitPrice = 85000m, TotalStock = 35 },
                new Product { SKU = "DC-001", ProductName = "Khuôn muffin 12 cốc", Description = "Khuôn muffin silicon 12 cốc, chống dính", UnitOfMeasure = "cái", Weight = 0.3m, UnitPrice = 75000m, TotalStock = 25 },
                new Product { SKU = "DC-002", ProductName = "Túi bắt kem + 6 đầu", Description = "Túi bắt kem silicone kèm 6 đầu trang trí", UnitOfMeasure = "bộ", Weight = 0.2m, UnitPrice = 65000m, TotalStock = 30 },
                new Product { SKU = "DC-003", ProductName = "Cây lăn bột gỗ (40cm)", Description = "Cây lăn bột bằng gỗ tự nhiên, dài 40cm", UnitOfMeasure = "cái", Weight = 0.5m, UnitPrice = 55000m, TotalStock = 40 },
                new Product { SKU = "DC-004", ProductName = "Giấy nến chống dính (10 tờ)", Description = "Giấy nến tráng silicone, dùng nướng bánh", UnitOfMeasure = "gói", Weight = 0.1m, UnitPrice = 28000m, TotalStock = 90 }
            };

            // Gán CategoryId
            sampleProducts[0].CategoryId = Find("Bột mì")!.CategoryId;
            sampleProducts[1].CategoryId = Find("Bột mì")!.CategoryId;
            sampleProducts[2].CategoryId = Find("Bột năng")!.CategoryId;
            sampleProducts[3].CategoryId = Find("Đường & Sữa")!.CategoryId;
            sampleProducts[4].CategoryId = Find("Đường & Sữa")!.CategoryId;
            sampleProducts[5].CategoryId = Find("Đường & Sữa")!.CategoryId;
            sampleProducts[6].CategoryId = Find("Bơ & Trứng")!.CategoryId;
            sampleProducts[7].CategoryId = Find("Bơ & Trứng")!.CategoryId;
            sampleProducts[8].CategoryId = Find("Màu thực phẩm")!.CategoryId;
            sampleProducts[9].CategoryId = Find("Màu thực phẩm")!.CategoryId;
            sampleProducts[10].CategoryId = Find("Màu thực phẩm")!.CategoryId;
            sampleProducts[11].CategoryId = Find("Khuôn bánh")!.CategoryId;
            sampleProducts[12].CategoryId = Find("Khuôn bánh")!.CategoryId;
            sampleProducts[13].CategoryId = Find("Khuôn bánh")!.CategoryId;
            sampleProducts[14].CategoryId = Find("Khuôn bánh")!.CategoryId;

            context.Products.AddRange(sampleProducts);
            await context.SaveChangesAsync();
        }

        // ============================================================
        // VOUCHER WELCOME10 (10% off, min 100k)
        // ============================================================
        private static async Task SeedSampleVoucherAsync(AppDbContext context)
        {
            if (await context.Vouchers.AnyAsync(v => v.VoucherCode == "WELCOME10")) return;

            context.Vouchers.Add(new Voucher
            {
                VoucherCode = "WELCOME10",
                DiscountType = DiscountType.Percent,
                DiscountValue = 10m,
                MinOrderValue = 100000m,
                MaxDiscountAmount = 50000m,
                StartDate = DateTime.UtcNow.AddDays(-7),
                EndDate = DateTime.UtcNow.AddDays(60),
                UsageLimit = 1000,
                UsedCount = 0,
                IsActive = true
            });
            await context.SaveChangesAsync();
        }
    }
}
