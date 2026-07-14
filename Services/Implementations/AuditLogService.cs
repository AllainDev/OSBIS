using OSBIS.Models.Entities;
using OSBIS.Models.Enums;
using OSBIS.Repositories.Interfaces;
using OSBIS.Services.Interfaces;

namespace OSBIS.Services.Implementations
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AuditLogService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task LogAsync(int? userId, string username, AuditAction action,
            string? description = null, bool isSuccess = true, string? errorMessage = null,
            string? controller = null, string? actionName = null, string? ipAddress = null)
        {
            try
            {
                await _unitOfWork.AuditLogs.LogAsync(userId, username, action, description,
                    isSuccess, errorMessage, controller, actionName, ipAddress);
                await _unitOfWork.SaveChangesAsync();
            }
            catch
            {
                // Không throw exception từ audit log để không làm crash app
            }
        }

        public async Task<IEnumerable<AuditLog>> GetRecentLogsAsync(int take = 100)
        {
            return await _unitOfWork.AuditLogs.GetAllAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetLogsByUserAsync(int userId, int take = 50)
        {
            return await _unitOfWork.AuditLogs.GetByUserIdAsync(userId, take);
        }
    }
}