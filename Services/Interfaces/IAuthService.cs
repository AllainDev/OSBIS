using OSBIS.Models.Entities;
using OSBIS.Models.ViewModels;

namespace OSBIS.Services.Interfaces
{
    /// <summary>
    /// Authentication Service - Xử lý đăng ký, đăng nhập, đăng xuất
    /// BR01: Tất cả tài khoản phải được xác thực để sử dụng
    /// BR06: Khóa tài khoản sau 5 lần đăng nhập sai liên tiếp (15 phút)
    /// </summary>
    public interface IAuthService
    {
        Task<AuthResult> RegisterAsync(RegisterViewModel model, string ipAddress);
        Task<AuthResult> LoginAsync(LoginViewModel model, string ipAddress);
        Task LogoutAsync(int userId, string ipAddress);
        Task<User?> GetCurrentUserAsync(System.Security.Claims.ClaimsPrincipal principal);
        Task<bool> IsAccountLockedAsync(int userId);
    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public User? User { get; set; }
        public int FailedAttemptsRemaining { get; set; }
        public DateTime? LockoutEnd { get; set; }
    }
}