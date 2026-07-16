using AspNetCoreRateLimit;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OSBIS.Data;
using OSBIS.Filters;
using OSBIS.Middleware;
using OSBIS.Repositories.Implementations;
using OSBIS.Repositories.Interfaces;
using OSBIS.Services.Helpers;
using OSBIS.Services.Implementations;
using OSBIS.Services.Interfaces;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// SERILOG CONFIGURATION
// ============================================================
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "Logs/osbis-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// ============================================================
// DATABASE
// ============================================================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ============================================================
// MVC + FILTERS
// ============================================================
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<GlobalExceptionFilter>();
    // OWASP A01: Auto-validate antiforgery token for ALL POST/PUT/DELETE
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
})
.AddRazorOptions(options =>
{
    // Hỗ trợ view nằm trong /Views/{Area}/{Controller}/{Action}.cshtml
    // (cấu trúc dự án hiện tại, thay vì /Areas/{Area}/Views/... chuẩn ASP.NET Core)
    options.AreaViewLocationFormats.Add("/Views/{2}/{1}/{0}.cshtml");
    options.AreaViewLocationFormats.Add("/Views/{2}/Shared/{0}.cshtml");
});

// ============================================================
// FLUENT VALIDATION
// ============================================================
builder.Services.AddValidatorsFromAssemblyContaining<OSBIS.Services.Validators.LoginValidator>();

// ============================================================
// REPOSITORIES (Unit of Work pattern)
// ============================================================
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IUserAddressRepository, UserAddressRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
// Phase 2
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IInventoryBatchRepository, InventoryBatchRepository>();
builder.Services.AddScoped<IProductImageRepository, ProductImageRepository>();
// Phase 3
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderDetailRepository, OrderDetailRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IVoucherRepository, VoucherRepository>();
builder.Services.AddScoped<IVoucherUsageRepository, VoucherUsageRepository>();
builder.Services.AddScoped<ISystemConfigRepository, SystemConfigRepository>();
// Phase 4
builder.Services.AddScoped<IShipmentRepository, ShipmentRepository>();
builder.Services.AddScoped<IShipmentTrackingRepository, ShipmentTrackingRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
// Phase 5
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

// ============================================================
// SERVICES
// ============================================================
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
// Phase 2
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IInventoryBatchService, InventoryBatchService>();
builder.Services.AddScoped<IProductImageService, ProductImageService>();
// Phase 3
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IVoucherService, VoucherService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ISystemConfigService, SystemConfigService>();
// Phase 4
builder.Services.AddScoped<IShipmentService, ShipmentService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
// Phase 5
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IReportService, ReportService>();

// Helpers
builder.Services.AddScoped<ImageUploadHelper>(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    return new ImageUploadHelper(env.WebRootPath);
});
// ShippingCalculator có cả async và static, KHÔNG cần DI
// OrderCodeGenerator cần IUnitOfWork, instance trực tiếp trong OrderService
// VoucherCalculator là static
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache(); // cho Phase 5 cache

// ============================================================
// COOKIE AUTHENTICATION
// ============================================================
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.Cookie.Name = "OSBIS.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.IsEssential = true;

        options.Events.OnSigningIn = context =>
        {
            context.Properties.IssuedUtc = DateTimeOffset.UtcNow;
            return Task.CompletedTask;
        };
    });

// ============================================================
// AUTHORIZATION POLICIES
// ============================================================
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireStaff", policy => policy.RequireRole("Admin", "Staff"));
    options.AddPolicy("RequireCustomer", policy => policy.RequireRole("Customer"));
    options.AddPolicy("RequireDelivery", policy => policy.RequireRole("Delivery"));
});

// ============================================================
// RATE LIMITING (BR06)
// ============================================================
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "OSBIS.Session";
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// ============================================================
// BACKGROUND JOBS (Phase 5)
// ============================================================
builder.Services.AddHostedService<OSBIS.Jobs.CartCleanupJob>();
builder.Services.AddHostedService<OSBIS.Jobs.ExpiringBatchNotificationJob>();

var app = builder.Build();

// ============================================================
// SEED DATABASE
// ============================================================
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        Log.Information("Applying database migrations...");
        await context.Database.MigrateAsync();

        Log.Information("Seeding database...");
        await DbSeeder.SeedAsync(context, passwordHasher);
        Log.Information("Database seeded successfully.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error during database initialization");
    }
}

// ============================================================
// MIDDLEWARE PIPELINE
// ============================================================
app.UseSerilogRequestLogging();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<SessionTimeoutMiddleware>();
app.UseMiddleware<AuditLogMiddleware>();
app.UseIpRateLimiting();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

try
{
    Log.Information("Starting OSBIS application");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
