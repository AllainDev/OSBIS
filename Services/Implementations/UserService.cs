using OSBIS.Common;
using OSBIS.Models.Entities;
using OSBIS.Models.Enums;
using OSBIS.Models.ViewModels;
using OSBIS.Repositories.Interfaces;
using OSBIS.Services.Interfaces;

namespace OSBIS.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAuditLogService _auditLogService;

        public UserService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, IAuditLogService auditLogService)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _auditLogService = auditLogService;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _unitOfWork.Users.GetAllAsync();
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _unitOfWork.Users.GetByIdAsync(id);
        }

        public async Task<User?> GetUserWithRoleAsync(int id)
        {
            return await _unitOfWork.Users.GetWithRoleAsync(id);
        }

        public async Task<AuthResult> CreateUserAsync(UserViewModel model, string ipAddress)
        {
            if (await _unitOfWork.Users.IsUsernameExistsAsync(model.Username))
                return new AuthResult { Success = false, Message = "Tên đăng nhập đã tồn tại." };

            if (await _unitOfWork.Users.IsEmailExistsAsync(model.Email))
                return new AuthResult { Success = false, Message = "Email đã được sử dụng." };

            var role = await _unitOfWork.Roles.GetByIdAsync(model.RoleId);
            if (role == null)
                return new AuthResult { Success = false, Message = "Role không hợp lệ." };

            var user = new User
            {
                Username = model.Username.Trim(),
                PasswordHash = _passwordHasher.HashPassword(model.Password ?? AppConstants.DefaultPassword),
                Email = model.Email.Trim().ToLower(),
                FullName = model.FullName.Trim(),
                Phone = model.Phone?.Trim(),
                RoleId = model.RoleId,
                IsActive = model.IsActive,
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                await _unitOfWork.BeginTransactionAsync();
                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.CommitTransactionAsync();

                await _auditLogService.LogAsync(null, "Admin", AuditAction.UserCreate,
                    description: $"Created user {user.Username} with role {role.RoleName}",
                    isSuccess: true, ipAddress: ipAddress);

                return new AuthResult { Success = true, Message = "Tạo người dùng thành công.", User = user };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                await _auditLogService.LogAsync(null, "Admin", AuditAction.UserCreate,
                    description: $"Failed to create user {model.Username}", isSuccess: false,
                    errorMessage: ex.Message, ipAddress: ipAddress);

                return new AuthResult { Success = false, Message = "Tạo người dùng thất bại." };
            }
        }

        public async Task<AuthResult> UpdateUserAsync(int id, UserViewModel model, string ipAddress)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null)
                return new AuthResult { Success = false, Message = "Người dùng không tồn tại." };

            // Kiểm tra email trùng (ngoại trừ chính user này)
            var emailExists = await _unitOfWork.Users.AnyAsync(u => u.Email == model.Email && u.UserId != id);
            if (emailExists)
                return new AuthResult { Success = false, Message = "Email đã được sử dụng." };

            user.FullName = model.FullName.Trim();
            user.Email = model.Email.Trim().ToLower();
            user.Phone = model.Phone?.Trim();
            user.RoleId = model.RoleId;
            user.IsActive = model.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            try
            {
                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                await _auditLogService.LogAsync(id, user.Username, AuditAction.UserUpdate,
                    description: "User updated", isSuccess: true, ipAddress: ipAddress);

                return new AuthResult { Success = true, Message = "Cập nhật thành công.", User = user };
            }
            catch (Exception ex)
            {
                await _auditLogService.LogAsync(id, user.Username, AuditAction.UserUpdate,
                    description: "Update failed", isSuccess: false, errorMessage: ex.Message,
                    ipAddress: ipAddress);

                return new AuthResult { Success = false, Message = "Cập nhật thất bại." };
            }
        }

        public async Task<bool> DeleteUserAsync(int id, string ipAddress)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null) return false;

            try
            {
                _unitOfWork.Users.Remove(user);
                await _unitOfWork.SaveChangesAsync();

                await _auditLogService.LogAsync(null, "Admin", AuditAction.UserDelete,
                    description: $"Deleted user {user.Username}", isSuccess: true, ipAddress: ipAddress);
                return true;
            }
            catch (Exception ex)
            {
                await _auditLogService.LogAsync(null, "Admin", AuditAction.UserDelete,
                    description: $"Failed to delete user {user.Username}", isSuccess: false,
                    errorMessage: ex.Message, ipAddress: ipAddress);
                return false;
            }
        }

        public async Task<bool> LockUserAsync(int id, DateTime lockoutEnd, string ipAddress)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null) return false;

            await _unitOfWork.Users.LockAccountAsync(id, lockoutEnd);
            await _unitOfWork.SaveChangesAsync();

            await _auditLogService.LogAsync(id, user.Username, AuditAction.UserLock,
                description: $"User locked until {lockoutEnd}", isSuccess: true, ipAddress: ipAddress);
            return true;
        }

        public async Task<bool> UnlockUserAsync(int id, string ipAddress)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null) return false;

            await _unitOfWork.Users.UnlockAccountAsync(id);
            await _unitOfWork.SaveChangesAsync();

            await _auditLogService.LogAsync(id, user.Username, AuditAction.UserUnlock,
                description: "User unlocked", isSuccess: true, ipAddress: ipAddress);
            return true;
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordViewModel model, string ipAddress)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null) return false;

            if (!_passwordHasher.VerifyPassword(model.CurrentPassword, user.PasswordHash))
            {
                await _auditLogService.LogAsync(userId, user.Username, AuditAction.PasswordChange,
                    description: "Wrong current password", isSuccess: false, ipAddress: ipAddress);
                return false;
            }

            user.PasswordHash = _passwordHasher.HashPassword(model.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            await _auditLogService.LogAsync(userId, user.Username, AuditAction.PasswordChange,
                description: "Password changed", isSuccess: true, ipAddress: ipAddress);
            return true;
        }
    }
}