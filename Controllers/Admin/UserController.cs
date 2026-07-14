using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OSBIS.Common;
using OSBIS.Models.Entities;
using OSBIS.Models.ViewModels;
using OSBIS.Repositories.Interfaces;
using OSBIS.Services.Interfaces;

namespace OSBIS.Controllers.Admin
{
    [Authorize(Roles = "Admin,Staff")]
    [Area("Admin")]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, IUnitOfWork unitOfWork, ILogger<UserController> logger)
        {
            _userService = userService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // GET: Admin/User
        public async Task<IActionResult> Index(string? search, string? role, int page = 1)
        {
            var allUsers = await _userService.GetAllUsersAsync();
            var usersQuery = allUsers.AsQueryable();

            // Lấy role cho mỗi user (eager load)
            var usersWithRoles = usersQuery.ToList();
            foreach (var u in usersWithRoles)
            {
                if (u.Role == null)
                {
                    u.Role = await _unitOfWork.Roles.GetByIdAsync(u.RoleId) ?? new Role();
                }
            }

            // Filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                usersWithRoles = usersWithRoles
                    .Where(u => u.Username.Contains(search, StringComparison.OrdinalIgnoreCase)
                             || u.Email.Contains(search, StringComparison.OrdinalIgnoreCase)
                             || u.FullName.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                usersWithRoles = usersWithRoles
                    .Where(u => u.Role.RoleName == role)
                    .ToList();
            }

            // Pagination
            const int pageSize = 20;
            var totalCount = usersWithRoles.Count;
            var pagedUsers = usersWithRoles
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var model = new UserListViewModel
            {
                Users = pagedUsers,
                SearchTerm = search,
                RoleFilter = role,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            ViewBag.Roles = new SelectList(
                await _unitOfWork.Roles.GetAllAsync(), "RoleName", "RoleName", role);

            return View(model);
        }

        // GET: Admin/User/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userService.GetUserWithRoleAsync(id);
            if (user == null)
            {
                TempData["Error"] = "Người dùng không tồn tại.";
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Admin/User/Create
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Roles = new SelectList(
                await _unitOfWork.Roles.GetAllAsync(), "RoleId", "RoleName");
            return View();
        }

        // POST: Admin/User/Create
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = new SelectList(
                    await _unitOfWork.Roles.GetAllAsync(), "RoleId", "RoleName", model.RoleId);
                return View(model);
            }

            var ip = HttpContext.GetClientIpAddress();
            var result = await _userService.CreateUserAsync(model, ip);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                ViewBag.Roles = new SelectList(
                    await _unitOfWork.Roles.GetAllAsync(), "RoleId", "RoleName", model.RoleId);
                return View(model);
            }

            TempData["Success"] = $"Đã tạo tài khoản '{model.Username}' với mật khẩu mặc định: {AppConstants.DefaultPassword}";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/User/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userService.GetUserWithRoleAsync(id);
            if (user == null)
            {
                TempData["Error"] = "Người dùng không tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            var model = new UserViewModel
            {
                UserId = user.UserId,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                RoleId = user.RoleId,
                IsActive = user.IsActive ?? true,
                RoleName = user.Role?.RoleName
            };

            ViewBag.Roles = new SelectList(
                await _unitOfWork.Roles.GetAllAsync(), "RoleId", "RoleName", model.RoleId);
            return View(model);
        }

        // POST: Admin/User/Edit/5
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UserViewModel model)
        {
            if (id != model.UserId)
            {
                TempData["Error"] = "ID không khớp.";
                return RedirectToAction(nameof(Index));
            }

            // Không validate password khi edit
            ModelState.Remove(nameof(model.Password));

            if (!ModelState.IsValid)
            {
                ViewBag.Roles = new SelectList(
                    await _unitOfWork.Roles.GetAllAsync(), "RoleId", "RoleName", model.RoleId);
                return View(model);
            }

            var ip = HttpContext.GetClientIpAddress();
            var result = await _userService.UpdateUserAsync(id, model, ip);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                ViewBag.Roles = new SelectList(
                    await _unitOfWork.Roles.GetAllAsync(), "RoleId", "RoleName", model.RoleId);
                return View(model);
            }

            TempData["Success"] = "Cập nhật thành công.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/User/Delete/5
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var ip = HttpContext.GetClientIpAddress();
            var success = await _userService.DeleteUserAsync(id, ip);

            TempData[success ? "Success" : "Error"] = success
                ? "Xóa người dùng thành công."
                : "Xóa thất bại.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/User/Lock/5
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Lock(int id, int minutes = 60)
        {
            var ip = HttpContext.GetClientIpAddress();
            var lockoutEnd = DateTime.UtcNow.AddMinutes(minutes);
            var success = await _userService.LockUserAsync(id, lockoutEnd, ip);

            TempData[success ? "Success" : "Error"] = success
                ? $"Đã khóa tài khoản trong {minutes} phút."
                : "Khóa thất bại.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/User/Unlock/5
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(int id)
        {
            var ip = HttpContext.GetClientIpAddress();
            var success = await _userService.UnlockUserAsync(id, ip);

            TempData[success ? "Success" : "Error"] = success
                ? "Đã mở khóa tài khoản."
                : "Mở khóa thất bại.";
            return RedirectToAction(nameof(Index));
        }
    }
}