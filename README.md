# OSBIS - Online Sales Business Information System

Hệ thống thương mại điện tử được xây dựng trên ASP.NET Core MVC 8.0 + Entity Framework Core 8.0.

## Tech Stack

- **.NET 8.0** (ASP.NET Core MVC)
- **Entity Framework Core 8.0.0** (SQL Server LocalDB)
- **BCrypt.Net-Next** - Password hashing
- **FluentValidation** - Input validation
- **Serilog** - Structured logging
- **AspNetCoreRateLimit** - Rate limiting (brute-force protection)
- **Bootstrap 5** + **AdminLTE 3** - UI

## Phase 1 - Authentication & User Management

### Tính năng
- ✅ Đăng ký / Đăng nhập / Đăng xuất
- ✅ Cookie Authentication (HttpOnly, Secure, SameSite=Lax)
- ✅ Authorization Policies (Admin, Staff, Customer, Delivery)
- ✅ Lockout sau 5 lần đăng nhập sai (BR06)
- ✅ Session timeout 30 phút (BR07)
- ✅ Security headers (CSP, X-Frame-Options, X-Content-Type-Options)
- ✅ Rate limiting cho login endpoint
- ✅ Audit log cho các action quan trọng
- ✅ Global exception handler
- ✅ Serilog file logging

### Cấu trúc thư mục

```
OSBIS/
├── Controllers/
│   ├── AccountController.cs       # Login, Register, Logout
│   └── Admin/
│       └── UserController.cs       # User management
├── Services/
│   ├── Interfaces/
│   └── Implementations/
├── Repositories/
│   ├── Interfaces/
│   └── Implementations/
├── Models/
│   ├── Entities/
│   ├── Enums/
│   └── ViewModels/
├── Filters/
├── Middleware/
├── Common/
└── Data/
    └── AppDbContext.cs
```

## Cài đặt

```bash
# Restore packages
dotnet restore

# Apply migrations
dotnet ef database update

# Run
dotnet run
```

## Tài khoản mặc định (sau khi seed)

| Username | Password | Role |
|----------|----------|------|
| admin | Admin@123 | Admin |