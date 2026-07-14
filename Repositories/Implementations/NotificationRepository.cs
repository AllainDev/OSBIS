using Microsoft.EntityFrameworkCore;
using OSBIS.Common;
using OSBIS.Data;
using OSBIS.Models.Entities;
using OSBIS.Repositories.Interfaces;

namespace OSBIS.Repositories.Implementations
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly AppDbContext _context;
        private readonly DbSet<Notification> _dbSet;

        public NotificationRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Notifications;
        }

        public async Task<PagedResult<Notification>> GetByUserAsync(int userId, int pageNumber, int pageSize)
        {
            var query = _dbSet.AsNoTracking().Where(n => n.UserId == userId);
            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Notification>
            {
                Items = items,
                TotalCount = total,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<IReadOnlyList<Notification>> GetLatestByUserAsync(int userId, int count)
        {
            return await _dbSet.AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _dbSet.CountAsync(n => n.UserId == userId && n.IsRead != true);
        }

        public async Task<Notification?> GetByIdAsync(int id)
        {
            return await _dbSet.FirstOrDefaultAsync(n => n.NotificationId == id);
        }

        public async Task AddAsync(Notification notification)
        {
            await _dbSet.AddAsync(notification);
        }

        public void Update(Notification notification)
        {
            _dbSet.Update(notification);
        }

        public void Remove(Notification notification)
        {
            _dbSet.Remove(notification);
        }

        public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}
