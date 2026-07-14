using OSBIS.Common;
using OSBIS.Models.Entities;

namespace OSBIS.Repositories.Interfaces
{
    /// <summary>Repository cho Review (Phase 4).</summary>
    public interface IReviewRepository
    {
        Task<Review?> GetByIdAsync(int id);
        Task<PagedResult<Review>> GetByProductAsync(int productId, int pageNumber, int pageSize);
        Task<IReadOnlyList<Review>> GetByOrderAsync(int orderId);

        /// <summary>Kiểm tra user đã review sản phẩm này trong order chưa (1 review per Order+Product).</summary>
        Task<bool> ExistsAsync(int orderId, int productId, int userId);

        Task<double> GetAverageRatingAsync(int productId);

        Task AddAsync(Review review);
        void Update(Review review);
        void Remove(Review review);

        Task<int> SaveChangesAsync();
    }
}
