# Phân quyền Admin vs Staff — OSBIS

## Tài khoản demo (seed sẵn)

| Role | Username | Password |
|---|---|---|
| Admin | `admin` | `Admin@123` |
| Staff | `staff` | `Staff@123` |
| Shipper | `shipper1` | `Shipper@123` |
| Customer | `customer1` | `User@123` |

## Ma trận phân quyền

| Chức năng | Admin | Staff | URL |
|---|:-:|:-:|---|
| **Tổng quan** | | | |
| Dashboard AdminLTE (charts, info-boxes) | ✅ | ✅ | `/Admin/Dashboard`, `/Staff/Dashboard` |
| Báo cáo (doanh thu, tồn kho) | ✅ | ❌ | `/Admin/Report/*` |
| **Vận hành** | | | |
| Xử lý đơn hàng | ✅ | ✅ | `/Staff/Order` |
| Quản lý vận chuyển | ✅ | ✅ | `/Staff/Shipment` |
| Quản lý sản phẩm | ✅ | ✅ | `/Staff/Product` |
| Quản lý voucher | ✅ | ✅ | `/Staff/Voucher` |
| **Danh mục** | | | |
| Quản lý danh mục SP | ✅ | ❌ | `/Admin/Category` |
| **Thông báo** | | | |
| Xem thông báo của tôi | ✅ | ✅ | `/Staff/Notification` hoặc `/Admin/Notification` |
| Gửi notification cho 1 user | ✅ | ❌ | `/Admin/Notification/SendToUser` |
| Broadcast tới tất cả customer | ✅ | ❌ | `/Admin/Notification/Broadcast` |
| **Quản trị (Admin only)** | | | |
| Quản lý người dùng (CRUD, khóa) | ✅ | ❌ | `/Admin/User` |
| Cấu hình hệ thống | ✅ | ❌ | `/Admin/SystemConfig` |

## Ghi chú thiết kế

- **Sidebar Admin** (`_LayoutAdminLTE`) chia 4 nhóm rõ ràng:
  1. **Tổng quan** (Dashboard, Báo cáo) — chỉ Admin
  2. **Vận hành** (Đơn, Sản phẩm, Voucher, Vận chuyển) — link sang `/Staff/*`
  3. **Danh mục** (Danh mục SP) — chỉ Admin
  4. **Thông báo** (của tôi) — cả 2
  5. **Quản trị** (User, SystemConfig, Send/Broadcast) — chỉ Admin, ẩn với Staff

- **Sidebar Staff** (`_LayoutStaff`) chỉ có 3 nhóm:
  1. **Tổng quan** (Dashboard)
  2. **Vận hành** (Order, Shipment, Product, Voucher)
  3. **Thông báo** (của tôi)
  - KHÔNG có menu Admin-only.

- **Authorize attributes** đã được set:
  - `[Authorize(Roles = "Admin")]` — UserController (chỉ Admin CRUD user)
  - `[Authorize(Roles = "Admin,Staff")]` — Order, Product, Shipment, Voucher, Notification, Dashboard
  - `[Authorize(Roles = "Admin")]` — SystemConfig, SendToUser, Broadcast

- **Layout switching** tự động theo `area` trong `Views/_ViewStart.cshtml`:
  - `area=Admin` → `_LayoutAdminLTE`
  - `area=Staff` → `_LayoutStaff`
  - khác → `_LayoutCustomer`

## Khắc phục UX đã làm

- Scroll position giữ nguyên khi chuyển trang (custom `history.scrollRestoration = 'manual'` + `beforeunload`/`pageshow`).
- `position: sticky` cho top navbar để giữ context khi cuộn dọc.
- `min-height` cho `.chart-card` để Chart.js không làm reflow khi render.
- `overflow-anchor` để trình duyệt neo vị trí cuộn.
- Sidebar collapse trên màn hình < 992px (toggle bằng nút hamburger).