using OSBIS.Common;
using OSBIS.Models.Entities;

namespace OSBIS.Repositories.Interfaces
{
    /// <summary>Repository cho Notification (Phase 5).</summary>
    public interface INotificationRepository
    {
        Task<PagedResult<Notification>> GetByUserAsync(int userId, int pageNumber, int pageSize);
        Task<IReadOnlyList<Notification>> GetLatestByUserAsync(int userId, int count);
        Task<int> GetUnreadCountAsync(int userId);
        Task<Notification?> GetByIdAsync(int id);

        Task AddAsync(Notification notification);
        void Update(Notification notification);
        void Remove(Notification notification);

        Task<int> SaveChangesAsync();
    }
}
