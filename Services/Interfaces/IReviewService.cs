using OSBIS.Common;
using OSBIS.Models.Entities;

namespace OSBIS.Services.Interfaces
{
    /// <summary>Service quản lý Review (Phase 4).</summary>
    public interface IReviewService
    {
        Task<PagedResult<Review>> GetByProductAsync(int productId, int pageNumber, int pageSize);
        Task<IReadOnlyList<Review>> GetByOrderAsync(int orderId);
        Task<double> GetAverageRatingAsync(int productId);
        Task<bool> CanReviewAsync(int userId, int orderId, int productId);

        Task<ReviewResult> CreateReviewAsync(int userId, int orderId, int productId, byte rating, string? comment);
        Task<ReviewResult> UpdateReviewAsync(int reviewId, int userId, byte rating, string? comment);
        Task<ReviewResult> DeleteReviewAsync(int reviewId, int userId);
    }

    public class ReviewResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Review? Review { get; set; }
    }
}
