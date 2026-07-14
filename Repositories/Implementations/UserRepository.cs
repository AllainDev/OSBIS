using Microsoft.EntityFrameworkCore;
using OSBIS.Data;
using OSBIS.Models.Entities;
using OSBIS.Repositories.Interfaces;

namespace OSBIS.Repositories.Implementations
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(AppDbContext context) : base(context) { }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _dbSet.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _dbSet.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail)
        {
            return await _dbSet.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == usernameOrEmail || u.Email == usernameOrEmail);
        }

        public async Task<User?> GetWithRoleAsync(int userId)
        {
            return await _dbSet
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<bool> IsUsernameExistsAsync(string username)
        {
            return await _dbSet.AnyAsync(u => u.Username == username);
        }

        public async Task<bool> IsEmailExistsAsync(string email)
        {
            return await _dbSet.AnyAsync(u => u.Email == email);
        }

        public async Task IncrementFailedLoginCountAsync(int userId)
        {
            var user = await _dbSet.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user != null)
            {
                user.FailedLoginCount++;
                user.UpdatedAt = DateTime.UtcNow;
                _dbSet.Update(user);
            }
        }

        public async Task ResetFailedLoginCountAsync(int userId)
        {
            var user = await _dbSet.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user != null)
            {
                user.FailedLoginCount = 0;
                user.LockoutEnd = null;
                user.UpdatedAt = DateTime.UtcNow;
                _dbSet.Update(user);
            }
            await Task.CompletedTask;
        }

        public async Task LockAccountAsync(int userId, DateTime lockoutEnd)
        {
            var user = await _dbSet.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user != null)
            {
                user.LockoutEnd = lockoutEnd;
                user.UpdatedAt = DateTime.UtcNow;
                _dbSet.Update(user);
            }
        }

        public async Task UnlockAccountAsync(int userId)
        {
            var user = await _dbSet.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user != null)
            {
                user.LockoutEnd = null;
                user.FailedLoginCount = 0;
                user.UpdatedAt = DateTime.UtcNow;
                _dbSet.Update(user);
            }
        }

        public async Task UpdateLastLoginAsync(int userId, string ipAddress)
        {
            var user = await _dbSet.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user != null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                user.LastLoginIp = ipAddress;
                user.UpdatedAt = DateTime.UtcNow;
                _dbSet.Update(user);
            }
        }

        public async Task<IEnumerable<User>> GetUsersByRoleAsync(string roleName)
        {
            return await _dbSet.AsNoTracking()
                .Include(u => u.Role)
                .Where(u => u.Role.RoleName == roleName)
                .ToListAsync();
        }
    }
}