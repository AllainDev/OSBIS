using OSBIS.Models.Entities;
using OSBIS.Models.ViewModels;

namespace OSBIS.Services.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> GetUserWithRoleAsync(int id);
        Task<AuthResult> CreateUserAsync(UserViewModel model, string ipAddress);
        Task<AuthResult> UpdateUserAsync(int id, UserViewModel model, string ipAddress);
        Task<bool> DeleteUserAsync(int id, string ipAddress);
        Task<bool> LockUserAsync(int id, DateTime lockoutEnd, string ipAddress);
        Task<bool> UnlockUserAsync(int id, string ipAddress);
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordViewModel model, string ipAddress);
    }
}