using OSBIS.Models.Entities;
using OSBIS.Models.Enums;

namespace OSBIS.Services.Interfaces
{
    public interface IAuditLogService
    {
        Task LogAsync(int? userId, string username, AuditAction action,
            string? description = null, bool isSuccess = true, string? errorMessage = null,
            string? controller = null, string? actionName = null, string? ipAddress = null);

        Task<IEnumerable<AuditLog>> GetRecentLogsAsync(int take = 100);
        Task<IEnumerable<AuditLog>> GetLogsByUserAsync(int userId, int take = 50);
    }
}