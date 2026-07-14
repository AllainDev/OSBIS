using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OSBIS.Common;
using OSBIS.Models.Entities;
using OSBIS.Models.Enums;
using OSBIS.Models.ViewModels;
using OSBIS.Repositories.Interfaces;
using OSBIS.Services.Interfaces;

namespace OSBIS.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAuditLogService _auditLogService;

        public AuthService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, IAuditLogService auditLogService)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _auditLogService = auditLogService;
        }

        public async Task<AuthResult> RegisterAsync(RegisterViewModel model, string ipAddress)
        {
            // Validate uniqueness
            if (await _unitOfWork.Users.IsUsernameExistsAsync(model.Username))
            {
                await _auditLogService.LogAsync(null, model.Username, AuditAction.Register,
                    description: "Username already exists", isSuccess: false, errorMessage: "Duplicate username");

                return new AuthResult { Success = false, Message = "Tên đăng nhập đã tồn tại." };
            }

            if (await _unitOfWork.Users.IsEmailExistsAsync(model.Email))
            {
                await _auditLogService.LogAsync(null, model.Username, AuditAction.Register,
                    description: "Email already exists", isSuccess: false, errorMessage: "Duplicate email");

                return new AuthResult { Success = false, Message = "Email đã được sử dụng." };
            }

            // Lấy role Customer
            var customerRole = await _unitOfWork.Roles.GetByNameAsync(AppConstants.Roles.Customer);
            if (customerRole == null)
            {
                return new AuthResult { Success = false, Message = "Lỗi hệ thống: Role Customer không tồn tại." };
            }

            // Tạo user mới
            var user = new User
            {
                Username = model.Username.Trim(),
                PasswordHash = _passwordHasher.HashPassword(model.Password),
                Email = model.Email.Trim().ToLower(),
                FullName = model.FullName.Trim(),
                Phone = model.Phone?.Trim(),
                RoleId = customerRole.RoleId,
                IsActive = true,
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                await _unitOfWork.BeginTransactionAsync();
                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.CommitTransactionAsync();

                await _auditLogService.LogAsync(user.UserId, user.Username, AuditAction.Register,
                    description: "User registered successfully",
                    isSuccess: true, ipAddress: ipAddress);

                return new AuthResult
                {
                    Success = true,
                    Message = "Đăng ký thành công.",
                    User = user
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                await _auditLogService.LogAsync(null, model.Username, AuditAction.Register,
                    description: "Registration failed", isSuccess: false, errorMessage: ex.Message);

                return new AuthResult { Success = false, Message = "Đăng ký thất bại. Vui lòng thử lại." };
            }
        }

        public async Task<AuthResult> LoginAsync(LoginViewModel model, string ipAddress)
        {
            var user = await _unitOfWork.Users.GetByUsernameOrEmailAsync(model.UsernameOrEmail.Trim());

            if (user == null)
            {
                await _auditLogService.LogAsync(null, model.UsernameOrEmail, AuditAction.LoginFailed,
                    description: "User not found", isSuccess: false,
                    ipAddress: ipAddress);

                return new AuthResult
                {
                    Success = false,
                    Message = "Tên đăng nhập hoặc mật khẩu không đúng.",
                    FailedAttemptsRemaining = AppConstants.Lockout.MaxFailedAttempts
                };
            }

            // BR06: Kiểm tra tài khoản bị khóa
            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            {
                var remainingMinutes = (int)(user.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes + 1;

                await _auditLogService.LogAsync(user.UserId, user.Username, AuditAction.LoginFailed,
                    description: $"Account locked. Remaining: {remainingMinutes} minutes",
                    isSuccess: false, ipAddress: ipAddress);

                return new AuthResult
                {
                    Success = false,
                    Message = $"Tài khoản đang bị khóa. Vui lòng thử lại sau {remainingMinutes} phút.",
                    LockoutEnd = user.LockoutEnd,
                    FailedAttemptsRemaining = 0
                };
            }

            // Reset lockout nếu đã hết hạn
            if (user.LockoutEnd.HasValue && user.LockoutEnd <= DateTime.UtcNow)
            {
                await _unitOfWork.Users.UnlockAccountAsync(user.UserId);
            }

            // Kiểm tra active
            if (user.IsActive == false)
            {
                await _auditLogService.LogAsync(user.UserId, user.Username, AuditAction.LoginFailed,
                    description: "Account deactivated", isSuccess: false, ipAddress: ipAddress);

                return new AuthResult { Success = false, Message = "Tài khoản đã bị vô hiệu hóa." };
            }

            // Verify password
            if (!_passwordHasher.VerifyPassword(model.Password, user.PasswordHash))
            {
                // Tăng failed count
                await _unitOfWork.Users.IncrementFailedLoginCountAsync(user.UserId);
                await _unitOfWork.SaveChangesAsync();

                var newFailedCount = user.FailedLoginCount + 1;
                var remaining = AppConstants.Lockout.MaxFailedAttempts - newFailedCount;

                // BR06: Khóa nếu vượt quá
                if (newFailedCount >= AppConstants.Lockout.MaxFailedAttempts)
                {
                    var lockoutEnd = DateTime.UtcNow.AddMinutes(AppConstants.Lockout.LockoutMinutes);
                    await _unitOfWork.Users.LockAccountAsync(user.UserId, lockoutEnd);
                    await _unitOfWork.SaveChangesAsync();

                    await _auditLogService.LogAsync(user.UserId, user.Username, AuditAction.Lockout,
                        description: $"Account locked for {AppConstants.Lockout.LockoutMinutes} minutes after {newFailedCount} failed attempts",
                        isSuccess: false, ipAddress: ipAddress);

                    return new AuthResult
                    {
                        Success = false,
                        Message = $"Tài khoản đã bị khóa {AppConstants.Lockout.LockoutMinutes} phút do đăng nhập sai quá {AppConstants.Lockout.MaxFailedAttempts} lần.",
                        LockoutEnd = lockoutEnd,
                        FailedAttemptsRemaining = 0
                    };
                }

                await _auditLogService.LogAsync(user.UserId, user.Username, AuditAction.LoginFailed,
                    description: $"Invalid password. Attempt {newFailedCount}/{AppConstants.Lockout.MaxFailedAttempts}",
                    isSuccess: false, ipAddress: ipAddress);

                return new AuthResult
                {
                    Success = false,
                    Message = $"Tên đăng nhập hoặc mật khẩu không đúng. Còn {remaining} lần thử.",
                    FailedAttemptsRemaining = remaining
                };
            }

            // Đăng nhập thành công - reset failed count
            await _unitOfWork.Users.ResetFailedLoginCountAsync(user.UserId);
            await _unitOfWork.Users.UpdateLastLoginAsync(user.UserId, ipAddress);
            await _unitOfWork.SaveChangesAsync();

            await _auditLogService.LogAsync(user.UserId, user.Username, AuditAction.Login,
                description: "Login successful", isSuccess: true, ipAddress: ipAddress);

            return new AuthResult
            {
                Success = true,
                Message = "Đăng nhập thành công.",
                User = user
            };
        }

        public async Task LogoutAsync(int userId, string ipAddress)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            await _auditLogService.LogAsync(userId, user?.Username ?? "Unknown", AuditAction.Logout,
                description: "User logged out", isSuccess: true, ipAddress: ipAddress);
        }

        public async Task<User?> GetCurrentUserAsync(ClaimsPrincipal principal)
        {
            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return null;

            return await _unitOfWork.Users.GetWithRoleAsync(userId);
        }

        public async Task<bool> IsAccountLockedAsync(int userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            return user?.LockoutEnd.HasValue == true && user.LockoutEnd > DateTime.UtcNow;
        }
    }
}