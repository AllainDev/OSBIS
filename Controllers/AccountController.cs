using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OSBIS.Common;
using OSBIS.Models.ViewModels;
using OSBIS.Services.Interfaces;

namespace OSBIS.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        private readonly ICartService _cartService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            IAuthService authService,
            IUserService userService,
            ICartService cartService,
            ILogger<AccountController> logger)
        {
            _authService = authService;
            _userService = userService;
            _cartService = cartService;
            _logger = logger;
        }

        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null, string? timeout = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var role = User.GetRole();
                return role switch
                {
                    AppConstants.Roles.Admin => Redirect("/Admin/Dashboard"),
                    AppConstants.Roles.Staff => Redirect("/Staff/Dashboard"),
                    _ => RedirectToAction("Index", "Home")
                };
            }

            ViewData["ReturnUrl"] = returnUrl;
            ViewData["Timeout"] = timeout;

            if (timeout == "1")
                TempData["Warning"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";

            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var ipAddress = HttpContext.GetClientIpAddress();
            var result = await _authService.LoginAsync(model, ipAddress);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                if (result.FailedAttemptsRemaining > 0 && result.FailedAttemptsRemaining <= 2)
                {
                    ModelState.AddModelError(string.Empty, $"Cảnh báo: Còn {result.FailedAttemptsRemaining} lần thử trước khi tài khoản bị khóa.");
                }
                return View(model);
            }

            // Tạo claims
            var user = result.User!;
            var role = await _userService.GetUserWithRoleAsync(user.UserId);
            var roleName = role?.Role?.RoleName ?? AppConstants.Roles.Customer;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, roleName),
                new Claim("FullName", user.FullName),
                new Claim("Phone", user.Phone ?? "")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe
                    ? DateTimeOffset.UtcNow.AddDays(7)
                    : DateTimeOffset.UtcNow.AddMinutes(AppConstants.SessionTimeoutMinutes),
                AllowRefresh = true
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Merge guest cart (theo session) vào user cart sau khi login
            var sessionCartId = HttpContext.Session.GetString("OSBIS_CartSessionId");
            if (!string.IsNullOrEmpty(sessionCartId))
            {
                try
                {
                    await _cartService.MergeCartOnLoginAsync(user.UserId, sessionCartId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Merge cart failed on login for user {UserId}", user.UserId);
                }
            }

            _logger.LogInformation("User {Username} logged in successfully", user.Username);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return roleName switch
            {
                AppConstants.Roles.Admin => Redirect("/Admin/Dashboard"),
                AppConstants.Roles.Staff => Redirect("/Staff/Dashboard"),
                _ => RedirectToAction("Index", "Home")
            };
        }

        // GET: /Account/Register
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var ipAddress = HttpContext.GetClientIpAddress();
            var result = await _authService.RegisterAsync(model, ipAddress);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userId = User.GetUserId() ?? 0;
            var ipAddress = HttpContext.GetClientIpAddress();

            await _authService.LogoutAsync(userId, ipAddress);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Clear session cart (guest trở lại)
            HttpContext.Session.Remove("OSBIS_CartSessionId");

            _logger.LogInformation("User {UserId} logged out", userId);
            TempData["Success"] = "Đăng xuất thành công.";
            return RedirectToAction(nameof(Login));
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied() => View();

        // GET: /Account/Profile
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userId = User.GetUserId();
            if (userId == null) return RedirectToAction(nameof(Login));

            var user = await _userService.GetUserWithRoleAsync(userId.Value);
            if (user == null) return RedirectToAction(nameof(Login));

            var model = new ProfileViewModel
            {
                UserId = user.UserId,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                RoleName = user.Role?.RoleName ?? "Customer",
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };

            return View(model);
        }

        // POST: /Account/Profile
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userVm = new UserViewModel
            {
                UserId = model.UserId,
                FullName = model.FullName,
                Email = model.Email,
                Phone = model.Phone,
                RoleId = 0,
                IsActive = true
            };

            var ipAddress = HttpContext.GetClientIpAddress();
            var result = await _userService.UpdateUserAsync(model.UserId, userVm, ipAddress);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            TempData["Success"] = "Cập nhật thông tin thành công.";
            return RedirectToAction(nameof(Profile));
        }

        // GET: /Account/ChangePassword
        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword() => View();

        // POST: /Account/ChangePassword
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = User.GetUserId();
            if (userId == null) return RedirectToAction(nameof(Login));

            var ipAddress = HttpContext.GetClientIpAddress();
            var success = await _userService.ChangePasswordAsync(userId.Value, model, ipAddress);

            if (!success)
            {
                ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng.");
                return View(model);
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Success"] = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại.";
            return RedirectToAction(nameof(Login));
        }
    }
}
