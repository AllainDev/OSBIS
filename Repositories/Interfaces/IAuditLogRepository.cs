using OSBIS.Models.Entities;
using OSBIS.Models.Enums;

namespace OSBIS.Repositories.Interfaces
{
    public interface IAuditLogRepository : IGenericRepository<AuditLog>
    {
        Task LogAsync(int? userId, string username, AuditAction action, string? description = null,
            bool isSuccess = true, string? errorMessage = null,
            string? controller = null, string? actionName = null,
            string? ipAddress = null, string? userAgent = null);

        Task<IEnumerable<AuditLog>> GetByUserIdAsync(int userId, int take = 50);
        Task<IEnumerable<AuditLog>> GetByActionAsync(AuditAction action, int take = 100);
    }
}