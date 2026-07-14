using Microsoft.EntityFrameworkCore;
using OSBIS.Common;
using OSBIS.Data;
using OSBIS.Models.Entities;
using OSBIS.Repositories.Interfaces;

namespace OSBIS.Repositories.Implementations
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly AppDbContext _context;
        private readonly DbSet<Review> _dbSet;

        public ReviewRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Reviews;
        }

        public async Task<Review?> GetByIdAsync(int id)
        {
            return await _dbSet
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.ReviewId == id);
        }

        public async Task<PagedResult<Review>> GetByProductAsync(int productId, int pageNumber, int pageSize)
        {
            var query = _dbSet.AsNoTracking()
                .Include(r => r.User)
                .Where(r => r.ProductId == productId);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(r => r.ReviewDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Review>
            {
                Items = items,
                TotalCount = total,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<IReadOnlyList<Review>> GetByOrderAsync(int orderId)
        {
            return await _dbSet.AsNoTracking()
                .Where(r => r.OrderId == orderId)
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(int orderId, int productId, int userId)
        {
            return await _dbSet.AnyAsync(r => r.OrderId == orderId && r.ProductId == productId && r.UserId == userId);
        }

        public async Task<double> GetAverageRatingAsync(int productId)
        {
            if (!await _dbSet.AnyAsync(r => r.ProductId == productId))
                return 0;
            return await _dbSet.Where(r => r.ProductId == productId).AverageAsync(r => (double)r.Rating);
        }

        public async Task AddAsync(Review review)
        {
            await _dbSet.AddAsync(review);
        }

        public void Update(Review review)
        {
            review.UpdatedAt = DateTime.UtcNow;
            _dbSet.Update(review);
        }

        public void Remove(Review review)
        {
            _dbSet.Remove(review);
        }

        public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}
