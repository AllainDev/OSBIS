using Microsoft.EntityFrameworkCore;
using OSBIS.Data;
using OSBIS.Models.Entities;
using OSBIS.Models.Enums;
using OSBIS.Repositories.Interfaces;

namespace OSBIS.Repositories.Implementations
{
    public class AuditLogRepository : GenericRepository<AuditLog>, IAuditLogRepository
    {
        public AuditLogRepository(AppDbContext context) : base(context) { }

        public async Task LogAsync(int? userId, string username, AuditAction action, string? description = null,
            bool isSuccess = true, string? errorMessage = null,
            string? controller = null, string? actionName = null,
            string? ipAddress = null, string? userAgent = null)
        {
            var log = new AuditLog
            {
                UserId = userId,
                Username = username,
                Action = action,
                Description = description,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                Controller = controller,
                ActionName = actionName,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow
            };

            await _dbSet.AddAsync(log);
            // Không SaveChangesAsync ở đây - để caller quyết định
        }

        public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(int userId, int take = 50)
        {
            return await Task.FromResult(
                _dbSet.AsNoTracking()
                    .Where(al => al.UserId == userId)
                    .OrderByDescending(al => al.CreatedAt)
                    .Take(take)
                    .ToList()
            );
        }

        public async Task<IEnumerable<AuditLog>> GetByActionAsync(AuditAction action, int take = 100)
        {
            return await Task.FromResult(
                _dbSet.AsNoTracking()
                    .Where(al => al.Action == action)
                    .OrderByDescending(al => al.CreatedAt)
                    .Take(take)
                    .ToList()
            );
        }
    }
}