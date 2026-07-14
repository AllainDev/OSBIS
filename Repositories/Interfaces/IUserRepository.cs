using OSBIS.Models.Entities;

namespace OSBIS.Repositories.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail);
        Task<User?> GetWithRoleAsync(int userId);
        Task<bool> IsUsernameExistsAsync(string username);
        Task<bool> IsEmailExistsAsync(string email);
        Task IncrementFailedLoginCountAsync(int userId);
        Task ResetFailedLoginCountAsync(int userId);
        Task LockAccountAsync(int userId, DateTime lockoutEnd);
        Task UnlockAccountAsync(int userId);
        Task UpdateLastLoginAsync(int userId, string ipAddress);
        Task<IEnumerable<User>> GetUsersByRoleAsync(string roleName);
    }
}